using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XerifeTv.CMS.Modules.AuditLog.Interfaces;
using XerifeTv.CMS.Modules.ContentCuration;
using XerifeTv.CMS.Modules.ContentCuration.Dtos.Request;
using XerifeTv.CMS.Modules.ContentCuration.Dtos.Response;
using XerifeTv.CMS.Modules.ContentCuration.Enums;
using XerifeTv.CMS.Modules.ContentCuration.Interfaces;
using XerifeTv.CMS.Modules.Franchise.Dtos.Response;
using XerifeTv.CMS.Modules.Franchise.Interfaces;
using XerifeTv.CMS.Shared.Extensions;
using XerifeTv.CMS.Shared.Helpers;
using XerifeTv.CMS.Modules.ContentCuration.Views.Models;

namespace XerifeTv.CMS.Modules.ContentCuration.Controllers;

[Authorize(Roles = "admin, common")]
public class ContentCurationController(
    IFeaturedService featuredService,
    ICurationCollectionService collectionService,
    IFranchiseService franchiseService,
    IAuditLogService auditLogService,
    ILogger<ContentCurationController> logger) : Controller
{
    private const int limitResultsPage = 20;

    public async Task<IActionResult> Index(int? currentPage)
    {
        logger.LogInformation($"{User.Identity?.Name} accessed the content curation categories page");

        var result = await collectionService.GetAsync(currentPage ?? 1, limitResultsPage);

        ViewBag.CurrentPage = result.Data?.CurrentPage ?? 1;
        ViewBag.TotalPages = result.Data?.TotalPageCount ?? 1;
        ViewBag.HasNextPage = result.Data?.HasNext;
        ViewBag.HasPrevPage = result.Data?.HasPrevious;

        return View(result.Data?.Items ?? Enumerable.Empty<GetCurationCollectionResponseDto>());
    }

    public async Task<IActionResult> Featured(int? currentPage)
    {
        logger.LogInformation($"{User.Identity?.Name} accessed the content curation featured page");

        var result = await featuredService.GetAsync(currentPage ?? 1, limitResultsPage);

        ViewBag.CurrentPage = result.Data?.CurrentPage ?? 1;
        ViewBag.TotalPages = result.Data?.TotalPageCount ?? 1;
        ViewBag.HasNextPage = result.Data?.HasNext;
        ViewBag.HasPrevPage = result.Data?.HasPrevious;

        return View(result.Data?.Items ?? Enumerable.Empty<GetFeaturedResponseDto>());
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> FeaturedForm(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            var response = await featuredService.GetAsync(id);

            if (response.IsSuccess)
                return View(new FeaturedFormModelView(response.Data));
        }

        return View(new FeaturedFormModelView(null));
    }

    [Authorize(Roles = "admin, common")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveFeatured(SaveFeaturedRequestDto dto)
    {
        var isCreate = string.IsNullOrWhiteSpace(dto.Id);
        var response = isCreate
            ? await featuredService.CreateAsync(dto)
            : await featuredService.UpdateAsync(dto);

        TempData["Notification"] = response.IsFailure
            ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
            : MessageViewHelper.SuccessJson(
                isCreate ? "Destaque cadastrado com sucesso" : "Destaque atualizado com sucesso");

        logger.LogInformation($"{User.Identity?.Name} saved the featured {dto.Title}");

        if (response.IsSuccess)
        {
            await this.AddAuditLogAsync(
                auditLogService,
                "ContentCurationFeatured",
                response.Data ?? dto.Id ?? string.Empty,
                $"{(isCreate ? "adicionou" : "atualizou")} o destaque {dto.Title}");
        }

        return RedirectToAction("Featured");
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> ToggleFeatured(string id)
    {
        var response = await featuredService.ToggleActiveAsync(id);

        TempData["Notification"] = response.IsFailure
            ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
            : MessageViewHelper.SuccessJson("Status do destaque alterado com sucesso");

        if (response.IsSuccess)
            await this.AddAuditLogAsync(auditLogService, "ContentCurationFeatured", id, "alterou o status do destaque");

        return RedirectToAction("Featured");
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> DeleteFeatured(string id)
    {
        var response = await featuredService.DeleteAsync(id);

        TempData["Notification"] = response.IsFailure
            ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
            : MessageViewHelper.SuccessJson("Destaque removido com sucesso");

        if (response.IsSuccess)
            await this.AddAuditLogAsync(auditLogService, "ContentCurationFeatured", id, "removeu o destaque");

        return RedirectToAction("Featured");
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> MoveFeatured(string id, int direction)
    {
        var response = await featuredService.MoveAsync(id, direction);

        if (response.IsFailure)
            TempData["Notification"] = MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty);

        return RedirectToAction("Featured");
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> CollectionForm(string? id)
    {
        var franchises = await LoadFranchiseOptionsAsync();

        if (!string.IsNullOrWhiteSpace(id))
        {
            var response = await collectionService.GetAsync(id);

            if (response.IsSuccess)
                return View(new CollectionFormModelView(response.Data, franchises));
        }

        return View(new CollectionFormModelView(null, franchises));
    }

    private async Task<IReadOnlyList<GetFranchiseResponseDto>> LoadFranchiseOptionsAsync()
    {
        var response = await franchiseService.GetAllAsync();

        if (!response.IsSuccess || response.Data is null)
            return [];

        return [.. response.Data.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)];
    }

    [Authorize(Roles = "admin, common")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCollection(SaveCurationCollectionRequestDto dto)
    {
        var isCreate = string.IsNullOrWhiteSpace(dto.Id);
        var response = isCreate
            ? await collectionService.CreateAsync(dto)
            : await collectionService.UpdateAsync(dto);

        TempData["Notification"] = response.IsFailure
            ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
            : MessageViewHelper.SuccessJson(
                isCreate ? "Coleção cadastrada com sucesso" : "Coleção atualizada com sucesso");

        logger.LogInformation($"{User.Identity?.Name} saved the curation collection {dto.Name}");

        if (response.IsSuccess)
        {
            await this.AddAuditLogAsync(
                auditLogService,
                "ContentCurationCollection",
                response.Data ?? dto.Id ?? string.Empty,
                $"{(isCreate ? "adicionou" : "atualizou")} a coleção {dto.Name}");
        }

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> ToggleCollection(string id)
    {
        var response = await collectionService.ToggleActiveAsync(id);

        TempData["Notification"] = response.IsFailure
            ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
            : MessageViewHelper.SuccessJson("Status da coleção alterado com sucesso");

        if (response.IsSuccess)
            await this.AddAuditLogAsync(auditLogService, "ContentCurationCollection", id, "alterou o status da coleção");

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> DeleteCollection(string id)
    {
        var response = await collectionService.DeleteAsync(id);

        TempData["Notification"] = response.IsFailure
            ? MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty)
            : MessageViewHelper.SuccessJson("Coleção removida com sucesso");

        if (response.IsSuccess)
            await this.AddAuditLogAsync(auditLogService, "ContentCurationCollection", id, "removeu a coleção");

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "admin, common")]
    public async Task<IActionResult> MoveCollection(string id, int direction)
    {
        var response = await collectionService.MoveAsync(id, direction);

        if (response.IsFailure)
            TempData["Notification"] = MessageViewHelper.ErrorJson(response.Error.Description ?? string.Empty);

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "admin, common")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderAutomatically()
    {
        var response = await collectionService.ReorderAutomaticallyAsync();

        if (response.IsFailure)
        {
            TempData["Notification"] = MessageViewHelper.ErrorJson(
                response.Error.Description ?? string.Empty);
        }
        else
        {
            var data = response.Data!;

            TempData["Notification"] = MessageViewHelper.SuccessJson(
                $"Categorias reorganizadas: {data.OrderedCount} ordenadas, " +
                $"{data.RedundantCount} redundantes e {data.EmptyCount} vazias ao final.");

            await this.AddAuditLogAsync(
                auditLogService,
                "ContentCurationCollection",
                "reorder",
                "reorganizou automaticamente as categorias da curadoria");
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> PreviewCollection(string id)
    {
        var response = await collectionService.PreviewAsync(id);

        if (response.IsFailure)
            return BadRequest(response.Error.Description ?? string.Empty);

        if (response.Data is null)
            return NotFound(new { error = "Coleção não encontrada." });

        return Ok(response.Data);
    }

    [HttpGet]
    public async Task<IActionResult> SearchContents(
        string? term, EContentType contentType = EContentType.MOVIE, int limit = 20)
    {
        var response = await collectionService.SearchContentsAsync(new SearchCurationContentRequestDto
        {
            Term = term ?? string.Empty,
            ContentType = contentType,
            Limit = limit
        });

        if (response.IsFailure)
            return BadRequest(response.Error.Description ?? string.Empty);

        return Ok(response.Data);
    }
}
