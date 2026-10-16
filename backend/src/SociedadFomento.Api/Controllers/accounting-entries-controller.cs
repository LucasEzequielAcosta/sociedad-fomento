using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SociedadFomento.Api.Contracts.Accounting;
using SociedadFomento.Api.Exports;
using SociedadFomento.Application.Abstractions;
using SociedadFomento.Application.Accounting;
using SociedadFomento.Application.Accounting.Dto;
using SociedadFomento.Application.Accounting.Models;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Api.Controllers;

/// <summary>Exposes administrative accounting-entry operations.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/accounting-entries")]
public sealed class AccountingEntriesController : ControllerBase
{
    private readonly AccountingEntryService accountingEntryService;
    private readonly IClock clock;

    /// <summary>Creates the accounting-entries controller.</summary>
    public AccountingEntriesController(AccountingEntryService accountingEntryService, IClock clock)
    {
        this.accountingEntryService = accountingEntryService;
        this.clock = clock;
    }

    /// <summary>Registers a manual income or expense.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AccountingEntryResponse>> Create(CreateManualAccountingEntryRequest request, CancellationToken cancellationToken)
    {
        AccountingEntryDto entry = await accountingEntryService.CreateManualAsync(
            new CreateManualAccountingEntryModel(request.EntryDate, request.Type, request.Concept, request.Amount),
            clock.UtcNow, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, ToResponse(entry));
    }

    /// <summary>Cancels a manual accounting entry while preserving its history.</summary>
    [HttpPost("{id:long}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(long id, CancellationToken cancellationToken)
    {
        await accountingEntryService.CancelAsync(id, clock.UtcNow, cancellationToken);
        return NoContent();
    }

    /// <summary>Returns accounting entries filtered by date range, type and concept.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountingEntryResponse>>> GetEntries(
        DateOnly? from, DateOnly? to, AccountingEntryType? type, string? concept, CancellationToken cancellationToken)
    {
        IReadOnlyList<AccountingEntryDto> entries = await accountingEntryService.GetEntriesAsync(
            new AccountingEntryFilter(from, to, type, concept), cancellationToken);
        return Ok(entries.Select(ToResponse).ToList());
    }

    /// <summary>Exports accounting entries as CSV filtered by date range, type and concept.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        DateOnly? from, DateOnly? to, AccountingEntryType? type, string? concept, CancellationToken cancellationToken)
    {
        IReadOnlyList<AccountingEntryDto> entries = await accountingEntryService.GetEntriesAsync(
            new AccountingEntryFilter(from, to, type, concept), cancellationToken);
        string csv = CsvWriter.Write(
            ["Id", "Fecha", "Tipo", "Origen", "Concepto", "Importe", "Estado"],
            entries.Select(entry => (IReadOnlyList<string?>)[
                entry.Id.ToString(),
                entry.EntryDate.ToString("yyyy-MM-dd"),
                entry.Type.ToString(),
                entry.Source.ToString(),
                entry.Concept,
                entry.Amount.ToString("F2"),
                entry.Status.ToString()]));
        return File(CsvWriter.ToUtf8Bytes(csv), "text/csv", "movimientos.csv");
    }

    /// <summary>Returns income, expense and balance totals for a named period or explicit date range.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<AccountingSummaryResponse>> GetSummary(
        AccountingSummaryPeriod? period, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        AccountingSummaryDto summary = await accountingEntryService.GetSummaryAsync(
            new GetAccountingSummaryModel(period, from, to), cancellationToken);
        return Ok(new AccountingSummaryResponse(summary.From, summary.To, summary.TotalIncome, summary.TotalExpense, summary.Balance));
    }

    /// <summary>Returns an accounting entry by identifier.</summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<AccountingEntryResponse>> GetById(long id, CancellationToken cancellationToken)
    {
        return Ok(ToResponse(await accountingEntryService.GetByIdAsync(id, cancellationToken)));
    }

    private static AccountingEntryResponse ToResponse(AccountingEntryDto dto) => new(
        dto.Id, dto.EntryDate, dto.Type, dto.Source, dto.Concept, dto.Amount,
        dto.Status, dto.PaymentId, dto.CancelledAtUtc, dto.CreatedAtUtc);
}
