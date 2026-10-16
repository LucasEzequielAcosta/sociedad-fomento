using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.FeeGeneration.Dto;
using SociedadFomento.Application.FeeGeneration.Models;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.FeeGeneration;

/// <summary>Coordinates generation of monthly fee obligations for active membership periods.</summary>
public sealed class FeeGenerationService
{
    private readonly IFeeGenerationContext context;

    /// <summary>Creates the fee-generation application service.</summary>
    public FeeGenerationService(IFeeGenerationContext context)
    {
        this.context = context;
    }

    /// <summary>Generates fee obligations for the supplied month without duplicating existing obligations.</summary>
    public Task<FeeGenerationResult> GenerateAsync(GenerateFeeObligationsModel model, DateTime createdAtUtc, CancellationToken cancellationToken = default)
    {
        DateOnly targetMonth = NormalizeMonth(model.Month);
        return context.ExecuteInTransactionAsync(async token =>
        {
            FeeRate rate = await GetApplicableRateAsync(targetMonth, token);
            IReadOnlyList<MembershipPeriod> periods = await context.GetPeriodsCoveringMonthAsync(targetMonth, token);
            if (periods.Count == 0)
            {
                return new FeeGenerationResult(targetMonth, 0);
            }

            HashSet<long> membersWithObligation = await context.GetMembersWithObligationAsync(targetMonth, token);
            List<FeeObligation> obligations = periods
                .DistinctBy(period => period.MemberId)
                .Where(period => !membersWithObligation.Contains(period.MemberId))
                .Select(period => new FeeObligation(period.Member, period, rate, targetMonth, createdAtUtc))
                .ToList();

            foreach (FeeObligation obligation in obligations)
            {
                context.AddObligation(obligation);
            }

            await context.SaveChangesAsync(token);
            return new FeeGenerationResult(targetMonth, obligations.Count);
        }, cancellationToken);
    }

    private async Task<FeeRate> GetApplicableRateAsync(DateOnly period, CancellationToken cancellationToken)
    {
        return await context.FindApplicableFeeRateAsync(period, cancellationToken)
            ?? throw new BusinessRuleException("FEE_RATE_NOT_FOUND", "No hay un valor de cuota vigente para el mes indicado.");
    }

    private static DateOnly NormalizeMonth(DateOnly value) => new(value.Year, value.Month, 1);
}
