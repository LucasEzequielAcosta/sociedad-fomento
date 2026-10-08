using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.MemberStatements.Dto;
using SociedadFomento.Application.MemberStatements.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.MemberStatements;

/// <summary>Coordinates construction of member account statements and debt calculation.</summary>
public sealed class MemberStatementService
{
    private readonly IMemberStatementContext context;

    /// <summary>Creates the member statement service.</summary>
    public MemberStatementService(IMemberStatementContext context)
    {
        this.context = context;
    }

    /// <summary>Produces an account statement for the supplied member as of the supplied date.</summary>
    public async Task<MemberStatementDto> GetAsync(GetMemberStatementModel model, CancellationToken cancellationToken = default)
    {
        Member? member = await context.GetMemberAsync(model.MemberId, cancellationToken);
        if (member is null)
        {
            throw new ResourceNotFoundException("MEMBER_NOT_FOUND", $"Member {model.MemberId} was not found.");
        }

        IReadOnlyList<FeeObligation> obligations = await context.GetObligationsAsync(model.MemberId, cancellationToken);
        IReadOnlyList<Payment> payments = await context.GetPaymentsAsync(model.MemberId, cancellationToken);

        List<MemberStatementObligationDto> obligationDtos = obligations
            .Select(obligation => new MemberStatementObligationDto(obligation.Id, obligation.Period, obligation.Amount, obligation.GetStatus(model.AsOf)))
            .OrderBy(dto => dto.Period)
            .ToList();

        List<MemberStatementPaymentDto> paymentDtos = payments
            .Select(payment => new MemberStatementPaymentDto(payment.Id, payment.PaymentDate, payment.Amount, payment.Method, payment.Status))
            .OrderByDescending(dto => dto.PaymentDate)
            .ToList();

        decimal totalPaid = paymentDtos.Where(dto => dto.Status == PaymentStatus.Active).Sum(dto => dto.Amount);
        decimal totalDebt = obligationDtos.Where(dto => dto.Status is FeeObligationStatus.Pending or FeeObligationStatus.Overdue)
            .Sum(dto => dto.Amount);

        return new MemberStatementDto(
            member.Id,
            member.Dni,
            member.FirstName,
            member.LastName,
            member.Status,
            obligationDtos,
            paymentDtos,
            totalPaid,
            totalDebt);
    }
}
