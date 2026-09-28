using System.ComponentModel.DataAnnotations;
using SociedadFomento.Domain.Constants;

namespace SociedadFomento.Api.Contracts.Members;

/// <summary>Represents an editable personal member data request.</summary>
public sealed record UpdateMemberPersonalDataRequest(
    [Required, MaxLength(MemberFieldLengths.FirstName)] string FirstName,
    [Required, MaxLength(MemberFieldLengths.LastName)] string LastName,
    [Required, MaxLength(MemberFieldLengths.Address)] string Address,
    DateOnly BirthDate,
    [Required, MaxLength(MemberFieldLengths.Phone)] string Phone,
    [EmailAddress, MaxLength(MemberFieldLengths.Email)] string? Email);
