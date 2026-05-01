using XerifeTv.CMS.Modules.Franchise;

namespace XerifeTv.CMS.Modules.Franchise.Dtos.Request;

public class UpdateFranchiseRequestDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    public Franchise ToEntity()
    {
        return new Franchise
        {
            Id = Id,
            Name = Name.Trim()
        };
    }
}
