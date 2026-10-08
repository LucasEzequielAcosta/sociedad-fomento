using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SociedadFomento.Api.Contracts.Payments;
using SociedadFomento.Application.Abstractions;
using SociedadFomento.Application.Payments;
using SociedadFomento.Application.Payments.Dto;
using SociedadFomento.Application.Payments.Models;

namespace SociedadFomento.Api.Controllers;

/// <summary>Exposes administrative payment operations.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly PaymentService paymentService;
    private readonly IClock clock;

    /// <summary>Creates the payments controller.</summary>
    public PaymentsController(PaymentService paymentService, IClock clock)
    {
        this.paymentService = paymentService;
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
}
