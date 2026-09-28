namespace SociedadFomento.Application.Members.Models;

/// <summary>Contains editable personal member data.</summary>
public sealed record MemberPersonalData(
    string FirstName,
    string LastName,
    string Address,
    DateOnly BirthDate,
    string Phone,
    string? Email);
