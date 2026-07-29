using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.AuditLog.Interfaces;
using XerifeTv.CMS.Modules.Franchise.Dtos.Request;
using XerifeTv.CMS.Modules.Franchise.Dtos.Response;
using XerifeTv.CMS.Modules.Franchise.Interfaces;
using XerifeTv.CMS.Shared.Extensions;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class FranchisesController(
    IFranchiseService service,
    IAuditLogService auditLogService,
    ILogger<FranchisesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Search(string? term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
            return Ok(Enumerable.Empty<GetFranchiseResponseDto>());

        var response = await service.SearchByNameAsync(term ?? string.Empty);

        if (response.IsFailure)
            return BadRequest(response.Error.Description ?? string.Empty);

        return Ok(response.Data);
    }

    [Authorize(Roles = "admin, common")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateFranchiseRequestDto dto)
    {
        var response = await service.CreateAsync(dto);

        if (response.IsFailure)
            return BadRequest(response.Error.Description ?? string.Empty);

        await this.AddAuditLogAsync(
            auditLogService,
            "Franchise",
            response.Data?.Id ?? string.Empty,
            $"adicionou a franquia {dto.Name}");

        logger.LogInformation($"{User.Identity?.Name} created franchise {dto.Name}");

        return Ok(response.Data);
    }

    [Authorize(Roles = "admin, common")]
    [HttpPost]
    public async Task<IActionResult> Update(UpdateFranchiseRequestDto dto)
    {
        var response = await service.UpdateAsync(dto);

        if (response.IsFailure)
            return BadRequest(response.Error.Description ?? string.Empty);

        await this.AddAuditLogAsync(
            auditLogService,
            "Franchise",
            response.Data?.Id ?? string.Empty,
            $"atualizou a franquia {dto.Name}");

        logger.LogInformation($"{User.Identity?.Name} updated franchise {dto.Id}");

        return Ok(response.Data);
    }

    [Authorize(Roles = "admin, common")]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        if (id is not null)
        {
            var franchiseResponse = await service.GetAsync(id);
            var name = franchiseResponse.IsSuccess && franchiseResponse.Data is not null
                ? franchiseResponse.Data.Name
                : id;

            var response = await service.DeleteAsync(id);

            if (response.IsFailure)
                return BadRequest(response.Error.Description ?? string.Empty);

            await this.AddAuditLogAsync(
                auditLogService,
                "Franchise",
                id,
                $"removeu a franquia {name}");

            logger.LogInformation($"{User.Identity?.Name} deleted franchise {id}");
        }

        return Ok();
    }
}

