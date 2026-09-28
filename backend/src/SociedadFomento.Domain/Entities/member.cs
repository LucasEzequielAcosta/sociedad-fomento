using SociedadFomento.Domain.Constants;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Domain.Entities;

/// <summary>Represents a society member and their activity lifecycle.</summary>
public sealed class Member
{
    private readonly List<MembershipPeriod> membershipPeriods = [];
    private readonly List<FeeObligation> feeObligations = [];
    private readonly List<Payment> payments = [];

    private Member() { }

    /// <summary>Registers an active member and opens the first membership period.</summary>
    public Member(string dni, string firstName, string lastName, string address, DateOnly birthDate, string phone, string? email, DateOnly registrationDate)
    {
        Dni = DniNormalizer.Normalize(dni);
        FirstName = EnsureRequired(firstName, MemberFieldLengths.FirstName, nameof(firstName));
        LastName = EnsureRequired(lastName, MemberFieldLengths.LastName, nameof(lastName));
        Address = EnsureRequired(address, MemberFieldLengths.Address, nameof(address));
        BirthDate = birthDate;
        Phone = EnsureRequired(phone, MemberFieldLengths.Phone, nameof(phone));
        Email = EnsureOptional(email, MemberFieldLengths.Email, nameof(email));
        RegistrationDate = registrationDate;
        Status = MemberStatus.Active;
        membershipPeriods.Add(new MembershipPeriod(this, registrationDate));
    }

    public long Id { get; private set; }
    public string Dni { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public DateOnly BirthDate { get; private set; }
    public string Phone { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public DateOnly RegistrationDate { get; private set; }
    public MemberStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<MembershipPeriod> MembershipPeriods => membershipPeriods;
    public IReadOnlyCollection<FeeObligation> FeeObligations => feeObligations;
    public IReadOnlyCollection<Payment> Payments => payments;

    /// <summary>Deactivates the member using the current system date supplied by the application.</summary>
    public void Deactivate(DateOnly currentDate)
    {
        MembershipPeriod period = GetOpenPeriod();
        period.Close(currentDate);
        Status = MemberStatus.Inactive;
    }

    /// <summary>Reactivates the member and opens a new membership period.</summary>
    public MembershipPeriod Reactivate(DateOnly currentDate)
    {
        if (Status != MemberStatus.Inactive)
        {
            throw new InvalidOperationException("Only an inactive member can be reactivated.");
        }

        DateOnly? lastEndDate = membershipPeriods.Max(period => period.EndDate);
        if (lastEndDate.HasValue && currentDate <= lastEndDate.Value)
        {
            throw new InvalidOperationException("Reactivation must occur after the latest deactivation date.");
        }

        MembershipPeriod period = new(this, currentDate);
        membershipPeriods.Add(period);
        Status = MemberStatus.Active;
        return period;
    }

    /// <summary>Updates the member fields that are not part of their historical identity.</summary>
    public void UpdatePersonalData(string firstName, string lastName, string address, DateOnly birthDate, string phone, string? email)
    {
        FirstName = EnsureRequired(firstName, MemberFieldLengths.FirstName, nameof(firstName));
        LastName = EnsureRequired(lastName, MemberFieldLengths.LastName, nameof(lastName));
        Address = EnsureRequired(address, MemberFieldLengths.Address, nameof(address));
        BirthDate = birthDate;
        Phone = EnsureRequired(phone, MemberFieldLengths.Phone, nameof(phone));
        Email = EnsureOptional(email, MemberFieldLengths.Email, nameof(email));
    }

    private MembershipPeriod GetOpenPeriod() => Status == MemberStatus.Active
        ? membershipPeriods.Single(period => !period.EndDate.HasValue)
        : throw new InvalidOperationException("Only an active member can be deactivated.");

    private static string EnsureRequired(string value, int maximumLength, string parameterName)
    {
        string normalized = value.Trim();
        return normalized.Length is > 0 && normalized.Length <= maximumLength
            ? normalized
            : throw new ArgumentException("The value is required and exceeds its allowed length.", parameterName);
    }

    private static string? EnsureOptional(string? value, int maximumLength, string parameterName) => string.IsNullOrWhiteSpace(value)
        ? null
        : EnsureRequired(value, maximumLength, parameterName);
}
