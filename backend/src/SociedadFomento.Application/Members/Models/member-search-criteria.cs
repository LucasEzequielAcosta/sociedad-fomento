namespace SociedadFomento.Application.Members.Models;

/// <summary>Defines filters and paging for member searches.</summary>
public sealed record MemberSearchCriteria(
    string? Dni = null,
    string? Name = null,
    string? LastName = null,
    int Page = 1,
    int PageSize = 20);
