namespace SociedadFomento.Application.Members.Dto;

/// <summary>Represents one page of application results.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
