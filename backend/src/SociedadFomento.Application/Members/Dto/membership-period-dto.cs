namespace SociedadFomento.Application.Members.Dto;

/// <summary>Represents one historical membership activity period.</summary>
public sealed record MembershipPeriodDto(long Id, DateOnly StartDate, DateOnly? EndDate);
