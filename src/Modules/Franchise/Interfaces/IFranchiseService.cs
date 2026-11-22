using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.Franchise.Dtos.Request;
using XerifeTv.CMS.Modules.Franchise.Dtos.Response;

namespace XerifeTv.CMS.Modules.Franchise.Interfaces;

public interface IFranchiseService
{
    Task<Result<IEnumerable<GetFranchiseResponseDto>>> GetAllAsync();
    Task<Result<IEnumerable<GetFranchiseResponseDto>>> SearchByNameAsync(string search);
    Task<Result<GetFranchiseResponseDto?>> GetByNameAsync(string name);
    Task<Result<GetFranchiseResponseDto?>> GetAsync(string id);
    Task<Result<GetFranchiseResponseDto>> CreateAsync(CreateFranchiseRequestDto dto);
    Task<Result<GetFranchiseResponseDto>> UpdateAsync(UpdateFranchiseRequestDto dto);
    Task<Result<bool>> DeleteAsync(string id);
}
