using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Domain.Tests;

/// <summary>Tests accounting entry behavior.</summary>
public sealed class AccountingEntryTests
{
    /// <summary>Verifies that automatic income uses exactly the originating payment date.</summary>
    [Fact]
    public void CreateForPayment_UsesPaymentDate()
    {
        Member member = TestData.CreateMember();
        FeeObligation obligation = TestData.CreateObligation(member, new DateOnly(2026, 9, 1));
        Payment payment = new(member, new DateOnly(2026, 9, 25), 5000m, PaymentMethod.BankTransfer, [obligation], TestData.CreatedAtUtc);

        AccountingEntry entry = AccountingEntry.CreateForPayment(payment, "Pago de cuotas", TestData.CreatedAtUtc);

        Assert.Equal(payment.PaymentDate, entry.EntryDate);
        Assert.Equal(AccountingEntrySource.FeePayment, entry.Source);
        Assert.Same(entry, payment.AccountingEntry);
    }

    /// <summary>Verifies that cancelling a manual entry retains it with an inactive status.</summary>
    [Fact]
    public void CancelManualEntry_PreservesHistoryAsCancelled()
    {
        AccountingEntry entry = AccountingEntry.CreateManual(
            new DateOnly(2026, 9, 25), AccountingEntryType.Expense, "Compra de insumos", 1000m, TestData.CreatedAtUtc);
        DateTime cancelledAtUtc = TestData.CreatedAtUtc.AddDays(1);

        entry.Cancel(cancelledAtUtc);

        Assert.Equal(AccountingEntryStatus.Cancelled, entry.Status);
        Assert.Equal(cancelledAtUtc, entry.CancelledAtUtc);
        Assert.Equal(1000m, entry.Amount);
    }
}
