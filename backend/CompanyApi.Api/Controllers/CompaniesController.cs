using CompanyApi.Application.Dtos;
using CompanyApi.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanyApi.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/companies")]
[Produces("application/json")]
public class CompaniesController(ICompanyService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CompanyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CompanyResponse>> Create(CreateCompanyRequest request)
    {
        var created = await service.CreateAsync(request, User.FindFirst("email")!.Value);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    [ProducesResponseType<PagedResponse<CompanyResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResponse<CompanyResponse>>> GetAll(
        [FromQuery] string? name, [FromQuery] string? domain, [FromQuery] PageRequest paging) =>
        Ok(await service.GetAllAsync(name, domain, paging));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CompanyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompanyResponse>> GetById(Guid id) => Ok(await service.GetByIdAsync(id));

    [HttpGet("search")]
    [ProducesResponseType<PagedResponse<CompanySearchResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<CompanySearchResult>>> Search(
        [FromQuery] string q, [FromQuery] string? domain, [FromQuery] PageRequest paging)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest(new ProblemDetails { Title = "Query 'q' is required.", Status = 400 });
        return Ok(await service.SearchAsync(q, domain, paging));
    }
}
