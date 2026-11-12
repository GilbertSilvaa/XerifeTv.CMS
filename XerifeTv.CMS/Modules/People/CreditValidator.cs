using XerifeTv.CMS.Modules.Abstractions.Enums;
using XerifeTv.CMS.Modules.Abstractions.ValueObjects;
using XerifeTv.CMS.Modules.Common;
using XerifeTv.CMS.Modules.People.Interfaces;

namespace XerifeTv.CMS.Modules.People;

public sealed class CreditValidator(IPersonRepository personRepository)
{
    public async Task<Result<bool>> ValidateAsync(IEnumerable<Credit>? credits)
    {
        var list = credits?.ToList() ?? [];

        if (list.Count == 0)
            return Result<bool>.Success(true);

        foreach (var credit in list)
        {
            if (!Enum.IsDefined(credit.Type))
                return Result<bool>.Failure(new Error("400", "Tipo de crédito inválido."));

            if (string.IsNullOrWhiteSpace(credit.PersonId))
                return Result<bool>.Failure(new Error("400", "Selecione a pessoa em todos os créditos."));

            if (credit.Type == ECreditType.Cast && string.IsNullOrWhiteSpace(credit.Character))
                return Result<bool>.Failure(new Error("400", "Informe o personagem em todos os créditos de elenco."));

            if (credit.Type == ECreditType.Crew &&
                (string.IsNullOrWhiteSpace(credit.Department) || string.IsNullOrWhiteSpace(credit.Job)))
            {
                return Result<bool>.Failure(
                    new Error("400", "Informe departamento e função em todos os créditos de equipe."));
            }
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var credit in list)
        {
            if (!keys.Add(CreditHelper.BuildKey(credit)))
                return Result<bool>.Failure(
                    new Error("409", "Existe um crédito equivalente para a mesma pessoa nesta obra."));
        }

        var personIds = list
            .Select(x => x.PersonId.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var people = await personRepository.GetByIdsAsync(personIds);
        var foundIds = people.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);

        if (personIds.Any(id => !foundIds.Contains(id)))
        {
            return Result<bool>.Failure(
                new Error("400", "Um dos créditos referencia uma pessoa não cadastrada. Atualize a página e tente novamente."));
        }

        return Result<bool>.Success(true);
    }
}
