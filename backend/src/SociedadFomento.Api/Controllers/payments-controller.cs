using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SociedadFomento.Api.Contracts.Payments;
using SociedadFomento.Api.Exports;
using SociedadFomento.Application.Abstractions;
using SociedadFomento.Application.Payments;
using SociedadFomento.Application.Payments.Dto;
using SociedadFomento.Application.Payments.Models;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Api.Controllers;

/// <summary>Exposes administrative payment operations.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly PaymentService paymentService;
    private readonly PaymentCancellationService paymentCancellationService;
    private readonly IClock clock;

    /// <summary>Creates the payments controller.</summary>
    public PaymentsController(PaymentService paymentService, PaymentCancellationService paymentCancellationService, IClock clock)
    {
        this.paymentService = paymentService;
        this.paymentCancellationService = paymentCancellationService;
        this.clock = clock;
    }

    /// <summary>Registers a payment allocated to complete fee obligations.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<PaymentDto>> Create(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        PaymentDto payment = await paymentService.RegisterAsync(
            new CreatePaymentModel(request.MemberId, request.PaymentDate, request.Amount, request.Method, request.ObligationIds),
            clock.UtcNow,
            cancellationToken);
        return Ok(payment);
    }

    /// <summary>Returns payments filtered by member, date range and payment method.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentListItemDto>>> GetPayments(
        long? memberId, DateOnly? from, DateOnly? to, PaymentMethod? method, CancellationToken cancellationToken)
    {
        IReadOnlyList<PaymentListItemDto> payments = await paymentService.GetPaymentsAsync(
            new PaymentFilter(memberId, from, to, method), cancellationToken);
        return Ok(payments);
    }

    /// <summary>Exports payments as CSV filtered by member, date range and payment method.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        long? memberId, DateOnly? from, DateOnly? to, PaymentMethod? method, CancellationToken cancellationToken)
    {
        IReadOnlyList<PaymentListItemDto> payments = await paymentService.GetPaymentsAsync(
            new PaymentFilter(memberId, from, to, method), cancellationToken);
        string csv = CsvWriter.Write(
            ["Id", "Dni", "Nombre", "Apellido", "Fecha", "Importe", "Medio", "Estado", "Periodos"],
            payments.Select(payment => (IReadOnlyList<string?>)[
                payment.Id.ToString(),
                payment.MemberDni,
                payment.MemberFirstName,
                payment.MemberLastName,
                payment.PaymentDate.ToString("yyyy-MM-dd"),
                payment.Amount.ToString("F2"),
                payment.Method.ToString(),
                payment.Status.ToString(),
                string.Join("; ", payment.Periods.Select(period => period.ToString("yyyy-MM")))]));
        return File(CsvWriter.ToUtf8Bytes(csv), "text/csv", "pagos.csv");
    }

    /// <summary>Cancels a payment, releasing its obligations and reversing its income.</summary>
    [HttpPost("{id:long}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(long id, CancellationToken cancellationToken)
    {
        await paymentCancellationService.CancelAsync(id, cancellationToken);
        return NoContent();
    }
}
