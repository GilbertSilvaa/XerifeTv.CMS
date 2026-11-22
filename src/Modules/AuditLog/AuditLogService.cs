using XerifeTv.CMS.Modules.AuditLog.Dtos.Request;
using XerifeTv.CMS.Modules.AuditLog.Dtos.Response;
using XerifeTv.CMS.Modules.AuditLog.Interfaces;
using XerifeTv.CMS.Modules.Common;

namespace XerifeTv.CMS.Modules.AuditLog;

public sealed class AuditLogService(IAuditLogRepository repository) : IAuditLogService
{
    public async Task<Result<PagedList<GetAuditLogResponseDto>>> GetAsync(int currentPage, int limit)
    {
        try
        {
            var response = await repository.GetAsync(currentPage, limit);

            var result = new PagedList<GetAuditLogResponseDto>(
              response.CurrentPage,
              response.TotalPageCount,
              response.Items.Select(GetAuditLogResponseDto.FromEntity));

            return Result<PagedList<GetAuditLogResponseDto>>.Success(result);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<PagedList<GetAuditLogResponseDto>>.Failure(error);
        }
    }

    public async Task<Result<PagedList<GetAuditLogResponseDto>>> GetAsync(string userId, int currentPage, int limit)
    {
        try
        {
            var response = await repository.GetAsync(userId, currentPage, limit);

            var result = new PagedList<GetAuditLogResponseDto>(
              response.CurrentPage,
              response.TotalPageCount,
              response.Items.Select(GetAuditLogResponseDto.FromEntity));

            return Result<PagedList<GetAuditLogResponseDto>>.Success(result);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<PagedList<GetAuditLogResponseDto>>.Failure(error);
        }
    }

    public async Task<Result<bool>> AddAsync(AddAuditLogRequestDto dto)
    {
        try
        {
            if (string.IsNullOrEmpty(dto.UserId) ||
                string.IsNullOrEmpty(dto.UserName) ||
                string.IsNullOrEmpty(dto.Description))
            {
                return Result<bool>.Failure(new Error("400", "UserId, UserName e Description são valores obrigatórios"));
            }

            var entity = dto.ToEntity();
            await repository.AddAsync(entity);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            var error = new Error("500", ex.InnerException?.Message ?? ex.Message);
            return Result<bool>.Failure(error);
        }
    }

}
