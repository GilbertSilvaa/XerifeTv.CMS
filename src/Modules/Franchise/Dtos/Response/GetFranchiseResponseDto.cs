using XerifeTv.CMS.Modules.Franchise;

namespace XerifeTv.CMS.Modules.Franchise.Dtos.Response;

public class GetFranchiseResponseDto
{
    public string Id { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    public static GetFranchiseResponseDto FromEntity(Franchise entity)
    {
        return new GetFranchiseResponseDto
        {
            Id = entity.Id,
            Name = entity.Name
        };
    }
}
