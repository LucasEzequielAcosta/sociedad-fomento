using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.Payments.Dto;
using SociedadFomento.Application.Payments.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Payments;

/// <summary>Coordinates registration of payments and their automatic accounting entries.</summary>
public sealed class PaymentService
{
    private readonly IPaymentContext context;

    /// <summary>Creates the payment application service.</summary>
    public PaymentService(IPaymentContext context)
    {
        this.context = context;
    }

    /// <summary>Registers a payment allocated to complete obligations and creates the automatic income entry.</summary>
    public async Task<PaymentDto> RegisterAsync(CreatePaymentModel model, DateTime createdAtUtc, CancellationToken cancellationToken = default)
    {
        ValidateModel(model);
        Member? member = await context.FindMemberAsync(model.MemberId, cancellationToken);
        if (member is null)
        {
            throw new ResourceNotFoundException("MEMBER_NOT_FOUND", $"Member {model.MemberId} was not found.");
        }

        List<FeeObligation> obligations = (await context.FindObligationsAsync(model.MemberId, model.ObligationIds, cancellationToken)).ToList();
        if (obligations.Count != model.ObligationIds.Count)
        {
            throw new RequestValidationException("INVALID_OBLIGATIONS", "One or more selected obligations do not belong to the member or do not exist.");
        }

        foreach (FeeObligation obligation in obligations)
        {
            if (await context.HasActiveAllocationAsync(obligation.Id, cancellationToken))
            {
                throw new BusinessRuleException("OBLIGATION_ALREADY_PAID", $"Obligation for {obligation.Period:yyyy-MM} is already paid.");
            }
        }

        Payment payment;
        try
        {
            payment = new Payment(member, model.PaymentDate, model.Amount, model.Method, obligations, createdAtUtc);
        }
        catch (ArgumentException exception)
        {
            throw new RequestValidationException("INVALID_PAYMENT", exception.Message);
        }

        AccountingEntry accountingEntry = AccountingEntry.CreateForPayment(payment, "Pago de cuota", createdAtUtc);
        return await context.ExecuteInTransactionAsync(async token =>
        {
            context.AddPayment(payment);
            context.AddAccountingEntry(accountingEntry);
            await context.SaveChangesAsync(token);
            return Map(member, payment);
        }, cancellationToken);
    }

    /// <summary>Returns payments matching the supplied member, date-range and method filters.</summary>
    public async Task<IReadOnlyList<PaymentListItemDto>> GetPaymentsAsync(PaymentFilter filter, CancellationToken cancellationToken = default)
    {
        if (filter.From is not null || filter.To is not null)
        {
            if (filter.From is null || filter.To is null || filter.From > filter.To)
            {
                throw new RequestValidationException("INVALID_PAYMENT_DATE_RANGE",
                    "Both dates are required and the start date cannot be after the end date.");
            }
        }

        if (filter.Method is not null && !Enum.IsDefined(typeof(PaymentMethod), filter.Method.Value))
        {
            throw new RequestValidationException("INVALID_PAYMENT_METHOD", "The payment method is not valid.");
        }

        return await context.GetPaymentsAsync(filter, cancellationToken);
    }

    private static void ValidateModel(CreatePaymentModel model)
    {
        if (model.ObligationIds.Count == 0)
        {
            throw new RequestValidationException("MISSING_OBLIGATIONS", "At least one fee obligation must be selected.");
        }

        if (model.Amount <= 0)
        {
            throw new RequestValidationException("INVALID_PAYMENT_AMOUNT", "Payment amount must be greater than zero.");
        }

        if (!Enum.IsDefined(typeof(PaymentMethod), model.Method))
        {
            throw new RequestValidationException("INVALID_PAYMENT_METHOD", "The payment method is not valid.");
        }

        HashSet<long> uniqueIds = new(model.ObligationIds);
        if (uniqueIds.Count != model.ObligationIds.Count)
        {
            throw new RequestValidationException("DUPLICATE_OBLIGATIONS", "Obligation identifiers must be unique.");
        }
    }

    private static PaymentDto Map(Member member, Payment payment) => new(
        payment.Id,
        member.Id,
        payment.PaymentDate,
        payment.Amount,
        payment.Method,
        payment.Status,
        payment.PaymentAllocations.Select(allocation => new PaymentAllocationDto(
            allocation.FeeObligationId,
            allocation.FeeObligation.Period,
            allocation.FeeObligation.Amount)).ToList());
}
