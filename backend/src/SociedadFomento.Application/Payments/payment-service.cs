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
            throw new ResourceNotFoundException("MEMBER_NOT_FOUND", $"No se encontró el socio {model.MemberId}.");
        }

        List<FeeObligation> obligations = (await context.FindObligationsAsync(model.MemberId, model.ObligationIds, cancellationToken)).ToList();
        if (obligations.Count != model.ObligationIds.Count)
        {
            throw new RequestValidationException("INVALID_OBLIGATIONS", "Una o más cuotas seleccionadas no pertenecen al socio o no existen.");
        }

        foreach (FeeObligation obligation in obligations)
        {
            if (await context.HasActiveAllocationAsync(obligation.Id, cancellationToken))
            {
                throw new BusinessRuleException("OBLIGATION_ALREADY_PAID", $"La cuota de {obligation.Period:yyyy-MM} ya está pagada.");
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
                    "Ambas fechas son obligatorias y la fecha inicial no puede ser posterior a la final.");
            }
        }

        if (filter.Method is not null && !Enum.IsDefined(typeof(PaymentMethod), filter.Method.Value))
        {
            throw new RequestValidationException("INVALID_PAYMENT_METHOD", "El medio de pago no es válido.");
        }

        return await context.GetPaymentsAsync(filter, cancellationToken);
    }

    private static void ValidateModel(CreatePaymentModel model)
    {
        if (model.ObligationIds.Count == 0)
        {
            throw new RequestValidationException("MISSING_OBLIGATIONS", "Debe seleccionar al menos una cuota.");
        }

        if (model.Amount <= 0)
        {
            throw new RequestValidationException("INVALID_PAYMENT_AMOUNT", "El importe del pago debe ser mayor a cero.");
        }

        if (!Enum.IsDefined(typeof(PaymentMethod), model.Method))
        {
            throw new RequestValidationException("INVALID_PAYMENT_METHOD", "El medio de pago no es válido.");
        }

        HashSet<long> uniqueIds = new(model.ObligationIds);
        if (uniqueIds.Count != model.ObligationIds.Count)
        {
            throw new RequestValidationException("DUPLICATE_OBLIGATIONS", "Las cuotas seleccionadas no pueden repetirse.");
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
