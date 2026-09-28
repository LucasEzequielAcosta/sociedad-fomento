using System.Net.Mail;
using SociedadFomento.Application.Abstractions;
using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.Members.Dto;
using SociedadFomento.Application.Members.Models;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Members;

/// <summary>Coordinates administrative member use cases.</summary>
public sealed class MemberService
{
    private readonly IMemberContext context;
    private readonly IClock clock;

    /// <summary>Creates the member application service.</summary>
    public MemberService(IMemberContext context, IClock clock)
    {
        this.context = context;
        this.clock = clock;
    }

    /// <summary>Registers a member and their current monthly obligation atomically.</summary>
    public Task<MemberDetailDto> CreateAsync(CreateMemberModel model, CancellationToken cancellationToken = default)
    {
        return context.ExecuteInTransactionAsync(token => CreateWithinTransactionAsync(model, token), cancellationToken);
    }

    /// <summary>Returns one member by internal identifier.</summary>
    public async Task<MemberDetailDto> GetByIdAsync(long memberId, CancellationToken cancellationToken = default)
    {
        return await context.GetByIdAsync(memberId, cancellationToken) ?? ThrowMemberDetailNotFound(memberId);
    }

    /// <summary>Returns one member by normalized DNI.</summary>
    public async Task<MemberDetailDto> GetByDniAsync(string dni, CancellationToken cancellationToken = default)
    {
        string normalizedDni = NormalizeDni(dni);
        return await context.GetByDniAsync(normalizedDni, cancellationToken)
            ?? throw new ResourceNotFoundException("MEMBER_NOT_FOUND", $"Member with DNI {normalizedDni} was not found.");
    }

    /// <summary>Searches members using optional filters and bounded pagination.</summary>
    public Task<PagedResult<MemberSummaryDto>> SearchAsync(MemberSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        ValidatePagination(criteria);
        MemberSearchCriteria normalized = criteria with
        {
            Dni = string.IsNullOrWhiteSpace(criteria.Dni) ? null : NormalizeDni(criteria.Dni),
            Name = NormalizeFilter(criteria.Name),
            LastName = NormalizeFilter(criteria.LastName)
        };
        return context.SearchAsync(normalized, cancellationToken);
    }

    /// <summary>Updates only editable personal member information.</summary>
    public async Task<MemberDetailDto> UpdatePersonalDataAsync(long memberId, MemberPersonalData data, CancellationToken cancellationToken = default)
    {
        ValidatePersonalData(data);
        Member member = await FindMemberAsync(memberId, cancellationToken);
        ApplyPersonalData(member, data);
        await context.SaveChangesAsync(cancellationToken);
        return Map(member);
    }

    /// <summary>Deactivates an active member using the current system date.</summary>
    public Task DeactivateAsync(long memberId, CancellationToken cancellationToken = default)
    {
        return context.ExecuteInTransactionAsync<object?>(async token =>
        {
            Member member = await FindMemberAsync(memberId, token);
            ExecuteLifecycleChange(() => member.Deactivate(clock.Today));
            await context.SaveChangesAsync(token);
            return null;
        }, cancellationToken);
    }

    /// <summary>Reactivates a member and creates the current obligation only when it does not already exist.</summary>
    public Task<ReactivateMemberResult> ReactivateAsync(long memberId, CancellationToken cancellationToken = default)
    {
        return context.ExecuteInTransactionAsync(token => ReactivateWithinTransactionAsync(memberId, token), cancellationToken);
    }

    /// <summary>Returns the complete activity period history for a member.</summary>
    public async Task<IReadOnlyList<MembershipPeriodDto>> GetMembershipHistoryAsync(long memberId, CancellationToken cancellationToken = default)
    {
        return await context.GetHistoryAsync(memberId, cancellationToken) ?? ThrowHistoryNotFound(memberId);
    }

    private async Task<MemberDetailDto> CreateWithinTransactionAsync(CreateMemberModel model, CancellationToken cancellationToken)
    {
        ValidatePersonalData(model.PersonalData);
        string normalizedDni = NormalizeDni(model.Dni);
        if (await context.DniExistsAsync(normalizedDni, cancellationToken))
        {
            throw new BusinessRuleException("DNI_DUPLICATE", "A member with the supplied DNI already exists.");
        }

        DateOnly currentDate = clock.Today;
        FeeRate rate = await GetApplicableRateAsync(currentDate, cancellationToken);
        Member member = CreateMember(normalizedDni, model.PersonalData, currentDate);
        MembershipPeriod period = member.MembershipPeriods.Single();
        FeeObligation obligation = new(member, period, rate, currentDate, clock.UtcNow);
        context.AddMember(member);
        context.AddObligation(obligation);
        await context.SaveChangesAsync(cancellationToken);
        return Map(member);
    }

    private async Task<ReactivateMemberResult> ReactivateWithinTransactionAsync(long memberId, CancellationToken cancellationToken)
    {
        Member member = await FindMemberAsync(memberId, cancellationToken);
        DateOnly currentDate = clock.Today;
        FeeRate rate = await GetApplicableRateAsync(currentDate, cancellationToken);
        FeeObligation? existing = await context.FindObligationAsync(memberId, NormalizeMonth(currentDate), cancellationToken);
        MembershipPeriod period = ExecuteLifecycleChange(() => member.Reactivate(currentDate));
        bool obligationCreated = existing is null;
        if (obligationCreated)
        {
            context.AddObligation(new FeeObligation(member, period, rate, currentDate, clock.UtcNow));
        }

        await context.SaveChangesAsync(cancellationToken);
        return new ReactivateMemberResult(Map(member), obligationCreated);
    }

    private async Task<Member> FindMemberAsync(long memberId, CancellationToken cancellationToken)
    {
        return await context.FindMemberForLifecycleAsync(memberId, cancellationToken) ?? ThrowMemberNotFound(memberId);
    }

    private async Task<FeeRate> GetApplicableRateAsync(DateOnly period, CancellationToken cancellationToken)
    {
        return await context.FindApplicableFeeRateAsync(NormalizeMonth(period), cancellationToken)
            ?? throw new BusinessRuleException("FEE_RATE_NOT_FOUND", "No fee rate is effective for the current month.");
    }

    private static Member CreateMember(string dni, MemberPersonalData data, DateOnly registrationDate)
    {
        try
        {
            return new Member(dni, data.FirstName, data.LastName, data.Address, data.BirthDate, data.Phone, data.Email, registrationDate);
        }
        catch (ArgumentException exception)
        {
            throw new RequestValidationException("INVALID_MEMBER_DATA", exception.Message);
        }
    }

    private static void ApplyPersonalData(Member member, MemberPersonalData data)
    {
        try
        {
            member.UpdatePersonalData(data.FirstName, data.LastName, data.Address, data.BirthDate, data.Phone, data.Email);
        }
        catch (ArgumentException exception)
        {
            throw new RequestValidationException("INVALID_MEMBER_DATA", exception.Message);
        }
    }

    private static T ExecuteLifecycleChange<T>(Func<T> operation)
    {
        try
        {
            return operation();
        }
        catch (InvalidOperationException exception)
        {
            throw new BusinessRuleException("MEMBER_STATE_CONFLICT", exception.Message);
        }
    }

    private static void ExecuteLifecycleChange(Action operation)
    {
        ExecuteLifecycleChange(() =>
        {
            operation();
            return true;
        });
    }

    private void ValidatePersonalData(MemberPersonalData data)
    {
        if (data.BirthDate == default || data.BirthDate > clock.Today)
        {
            throw new RequestValidationException("INVALID_BIRTH_DATE", "Birth date is required and cannot be in the future.");
        }
        if (data.Email is not null && !MailAddress.TryCreate(data.Email, out _))
        {
            throw new RequestValidationException("INVALID_EMAIL", "Email address is invalid.");
        }
    }

    private static void ValidatePagination(MemberSearchCriteria criteria)
    {
        if (criteria.Page < 1 || criteria.PageSize is < 1 or > 100)
        {
            throw new RequestValidationException("INVALID_PAGINATION", "Page must be positive and page size must be between 1 and 100.");
        }
    }

    private static string NormalizeDni(string dni)
    {
        try
        {
            return DniNormalizer.Normalize(dni);
        }
        catch (ArgumentException exception)
        {
            throw new RequestValidationException("INVALID_DNI", exception.Message);
        }
    }

    private static MemberDetailDto Map(Member member) => new(
        member.Id, member.Dni, member.FirstName, member.LastName, member.Address, member.BirthDate,
        member.Phone, member.Email, member.RegistrationDate, member.Status);

    private static DateOnly NormalizeMonth(DateOnly value) => new(value.Year, value.Month, 1);
    private static string? NormalizeFilter(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static Member ThrowMemberNotFound(long memberId) => throw new ResourceNotFoundException("MEMBER_NOT_FOUND", $"Member {memberId} was not found.");
    private static MemberDetailDto ThrowMemberDetailNotFound(long memberId) => throw new ResourceNotFoundException("MEMBER_NOT_FOUND", $"Member {memberId} was not found.");
    private static IReadOnlyList<MembershipPeriodDto> ThrowHistoryNotFound(long memberId) => throw new ResourceNotFoundException("MEMBER_NOT_FOUND", $"Member {memberId} was not found.");
}
