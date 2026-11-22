using XerifeTv.CMS.Modules.People.Interfaces;

namespace XerifeTv.CMS.Modules.People.Specifications;

public sealed class UniqueTmdbIdSpecification(IPersonRepository repository)
{
    public async Task<bool> IsSatisfiedByAsync(PersonEntity entity)
    {
        if (entity.TmdbId is null)
            return true;

        var existing = await repository.GetByTmdbIdAsync(entity.TmdbId.Value);

        return existing is null || existing.Id == entity.Id;
    }
}
