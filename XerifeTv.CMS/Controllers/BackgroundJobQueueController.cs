using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Dtos.Request;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Shared.Helpers;

namespace XerifeTv.CMS.Controllers;

[Authorize]
public class BackgroundJobQueueController(IBackgroundJobQueueService _service) : Controller
{
    [HttpPost]
    public async Task<IActionResult> AddJobInQueueSpreadsheetRegisters(AddSpreadsheetJobQueueRequestDto dto)
    {
        dto.RequestedByUsername = User?.Identity?.Name ?? string.Empty;
        var response = await _service.AddJobInQueueAsync(dto);

        if (response.IsFailure) return BadRequest(response.Error.Description);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson($"Processo adicionado a fila com sucesso");

        return Ok(response.Data);
    }

    [HttpPost]
    public async Task<IActionResult> AddJobInQueueImportEpisodesSeries(AddImportEpisodesJobQueueRequestDto dto)
    {
        dto.RequestedByUsername = User?.Identity?.Name ?? string.Empty;
        var response = await _service.AddJobInQueueAsync(dto);

        if (response.IsFailure) return BadRequest(response.Error.Description);

        TempData["Notification"] = response.IsFailure
          ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
          : MessageViewHelper.SuccessJson($"Processo adicionado a fila com sucesso");

        return Ok(response.Data);
    }
}
