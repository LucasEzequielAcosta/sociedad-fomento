namespace SociedadFomento.Domain.Entities;

/// <summary>Represents one continuous period in which a member is active.</summary>
public sealed class MembershipPeriod
{
    private MembershipPeriod() { }

    internal MembershipPeriod(Member member, DateOnly startDate)
    {
        Member = member;
        StartDate = startDate;
    }

    public long Id { get; private set; }
    public long MemberId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public Member Member { get; private set; } = null!;
    public IReadOnlyCollection<FeeObligation> FeeObligations => feeObligations;

    private readonly List<FeeObligation> feeObligations = [];

    /// <summary>Closes this activity period using the current system date supplied by the caller.</summary>
    public void Close(DateOnly currentDate)
    {
        if (EndDate.HasValue || currentDate < StartDate)
        {
            throw new InvalidOperationException("El período de membresía no puede cerrarse en la fecha indicada.");
        }

        EndDate = currentDate;
    }
}
