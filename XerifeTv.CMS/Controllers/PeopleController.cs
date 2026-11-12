using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.AuditLog.Interfaces;
using XerifeTv.CMS.Modules.People.Dtos.Request;
using XerifeTv.CMS.Modules.People.Interfaces;
using XerifeTv.CMS.Shared.Extensions;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class PeopleController(
    IPersonService service,
    IAuditLogService auditLogService,
    ILogger<PeopleController> logger) : Controller
{
    [Authorize(Roles = "admin, common")]
    [HttpGet]
    public async Task<IActionResult> Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest("Pessoa inválida.");

        var response = await service.GetAsync(id);

        return response.IsFailure
            ? BadRequest(response.Error.Description ?? string.Empty)
            : Ok(response.Data);
    }

    [Authorize(Roles = "admin, common")]
    [HttpPost]
    public async Task<IActionResult> Update(UpdatePersonRequestDto dto)
    {
        var response = await service.UpdateAsync(dto);

        if (response.IsFailure)
            return BadRequest(response.Error.Description ?? string.Empty);

        await this.AddAuditLogAsync(
            auditLogService,
            "Person",
            response.Data?.Id ?? dto.Id,
            $"atualizou a pessoa {response.Data?.Name ?? dto.Name}");

        logger.LogInformation($"{User.Identity?.Name} updated person {dto.Id}");

        return Ok(response.Data);
    }

    [Authorize(Roles = "admin, common")]
    [HttpGet]
    public async Task<IActionResult> SearchTmdb(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2)
            return Ok(Array.Empty<object>());

        var response = await service.SearchTmdbAsync(name);

        return response.IsFailure
            ? BadRequest(response.Error.Description ?? string.Empty)
            : Ok(response.Data);
    }

    [Authorize(Roles = "admin, common")]
    [HttpPost]
    public async Task<IActionResult> ImportPerson(int tmdbId)
    {
        if (tmdbId <= 0) return BadRequest("TMDB Person ID inválido.");

        var response = await service.ImportPersonAsync(tmdbId);

        if (response.IsFailure)
            return BadRequest(response.Error.Description ?? string.Empty);

        logger.LogInformation($"{User.Identity?.Name} imported person {tmdbId} from TMDB");

        return Ok(response.Data);
    }

    [Authorize(Roles = "admin, common")]
    [HttpPost]
    public async Task<IActionResult> RefreshFromTmdb(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest("Pessoa inválida.");

        var response = await service.RefreshFromTmdbAsync(id);

        if (response.IsFailure)
            return BadRequest(response.Error.Description ?? string.Empty);

        logger.LogInformation($"{User.Identity?.Name} refreshed person {id} from TMDB");

        return Ok(response.Data);
    }
}
