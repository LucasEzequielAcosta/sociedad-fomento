using System.ComponentModel.DataAnnotations;
using SociedadFomento.Domain.Constants;

namespace SociedadFomento.Api.Contracts.Members;

/// <summary>Represents a member registration request.</summary>
public sealed record CreateMemberRequest(
    [Required, MaxLength(MemberFieldLengths.Dni)] string Dni,
    [Required, MaxLength(MemberFieldLengths.FirstName)] string FirstName,
    [Required, MaxLength(MemberFieldLengths.LastName)] string LastName,
    [Required, MaxLength(MemberFieldLengths.Address)] string Address,
    DateOnly BirthDate,
    [Required, MaxLength(MemberFieldLengths.Phone)] string Phone,
    [EmailAddress, MaxLength(MemberFieldLengths.Email)] string? Email);
