using XerifeTv.CMS.Modules.Franchise;

namespace XerifeTv.CMS.Modules.Franchise.Dtos.Request;

public class CreateFranchiseRequestDto
{
    public string Name { get; init; } = string.Empty;

    public Franchise ToEntity()
    {
        return new Franchise
        {
            Name = Name.Trim()
        };
    }
}
