using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SociedadFomento.Api.Contracts.Members;
using SociedadFomento.Application.Members;
using SociedadFomento.Application.Members.Dto;
using SociedadFomento.Application.Members.Models;

namespace SociedadFomento.Api.Controllers;

/// <summary>Exposes administrative member operations.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/members")]
public sealed class MembersController : ControllerBase
{
    private readonly MemberService memberService;

    /// <summary>Creates the members controller.</summary>
    public MembersController(MemberService memberService)
    {
        this.memberService = memberService;
    }

    /// <summary>Registers an active member.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<MemberDetailDto>> Create(CreateMemberRequest request, CancellationToken cancellationToken)
    {
        MemberDetailDto member = await memberService.CreateAsync(ToModel(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = member.Id }, member);
    }

    /// <summary>Returns a member by identifier.</summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<MemberDetailDto>> GetById(long id, CancellationToken cancellationToken)
    {
        return Ok(await memberService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>Returns a member by DNI.</summary>
    [HttpGet("by-dni/{dni}")]
    public async Task<ActionResult<MemberDetailDto>> GetByDni(string dni, CancellationToken cancellationToken)
    {
        return Ok(await memberService.GetByDniAsync(dni, cancellationToken));
    }

    /// <summary>Searches and paginates members.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<MemberSummaryDto>>> Search(
        [FromQuery] string? dni,
        [FromQuery] string? name,
        [FromQuery] string? lastName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        MemberSearchCriteria criteria = new(dni, name, lastName, page, pageSize);
        return Ok(await memberService.SearchAsync(criteria, cancellationToken));
    }

    /// <summary>Updates editable personal member data.</summary>
    [HttpPut("{id:long}/personal-data")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<MemberDetailDto>> UpdatePersonalData(
        long id, UpdateMemberPersonalDataRequest request, CancellationToken cancellationToken)
    {
        return Ok(await memberService.UpdatePersonalDataAsync(id, ToModel(request), cancellationToken));
    }

    /// <summary>Deactivates an active member.</summary>
    [HttpPost("{id:long}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(long id, CancellationToken cancellationToken)
    {
        await memberService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Reactivates an inactive member.</summary>
    [HttpPost("{id:long}/reactivate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ReactivateMemberResult>> Reactivate(long id, CancellationToken cancellationToken)
    {
        return Ok(await memberService.ReactivateAsync(id, cancellationToken));
    }

    /// <summary>Returns the membership activity history.</summary>
    [HttpGet("{id:long}/membership-periods")]
    public async Task<ActionResult<IReadOnlyList<MembershipPeriodDto>>> GetMembershipHistory(
        long id, CancellationToken cancellationToken)
    {
        return Ok(await memberService.GetMembershipHistoryAsync(id, cancellationToken));
    }

    private static CreateMemberModel ToModel(CreateMemberRequest request) => new(
        request.Dni,
        new MemberPersonalData(request.FirstName, request.LastName, request.Address, request.BirthDate, request.Phone, request.Email));

    private static MemberPersonalData ToModel(UpdateMemberPersonalDataRequest request) => new(
        request.FirstName, request.LastName, request.Address, request.BirthDate, request.Phone, request.Email);
}
