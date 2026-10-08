using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SociedadFomento.Api.Contracts.MemberStatements;
using SociedadFomento.Application.Abstractions;
using SociedadFomento.Application.MemberStatements;
using SociedadFomento.Application.MemberStatements.Dto;
using SociedadFomento.Application.MemberStatements.Models;

namespace SociedadFomento.Api.Controllers;

/// <summary>Exposes administrative member account-statement operations.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/member-statements")]
public sealed class MemberStatementsController : ControllerBase
{
    private readonly MemberStatementService memberStatementService;
    private readonly IClock clock;

    /// <summary>Creates the member-statements controller.</summary>
    public MemberStatementsController(MemberStatementService memberStatementService, IClock clock)
    {
        this.memberStatementService = memberStatementService;
        this.clock = clock;
    }

    /// <summary>Returns the account statement for the supplied member as of today or the supplied date.</summary>
    [HttpGet("{memberId:long}")]
    public async Task<ActionResult<MemberStatementResponse>> Get(long memberId, [FromQuery] DateOnly? asOf, CancellationToken cancellationToken)
    {
        MemberStatementDto statement = await memberStatementService.GetAsync(
            new GetMemberStatementModel(memberId, asOf ?? clock.Today), cancellationToken);
        return Ok(new MemberStatementResponse(
            statement.MemberId,
            statement.Dni,
            statement.FirstName,
            statement.LastName,
            statement.Status.ToString(),
            statement.Obligations,
            statement.Payments,
            statement.TotalPaid,
            statement.TotalDebt));
    }
}
