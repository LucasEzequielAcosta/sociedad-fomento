using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Members.Dto;

/// <summary>Represents complete personal and lifecycle member information.</summary>
public sealed record MemberDetailDto(
    long Id,
    string Dni,
    string FirstName,
    string LastName,
    string Address,
    DateOnly BirthDate,
    string Phone,
    string? Email,
    DateOnly RegistrationDate,
    MemberStatus Status);
