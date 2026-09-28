using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Members.Dto;

/// <summary>Represents a member in search results.</summary>
public sealed record MemberSummaryDto(
    long Id,
    string Dni,
    string FirstName,
    string LastName,
    MemberStatus Status);
