using System.Globalization;
using XerifeTv.CMS.Modules.ContentCuration.Entities;
using XerifeTv.CMS.Modules.ContentCuration.Enums;

namespace XerifeTv.CMS.Modules.ContentCuration.Filters;

public static class CurationFilterValidator
{
    public static IReadOnlyList<string> Validate(
        IReadOnlyList<DynamicFilterRule> rules,
        IReadOnlyList<EContentType> contentTypes)
    {
        var errors = new List<string>();

        if (contentTypes.Count == 0)
        {
            errors.Add("Selecione pelo menos um tipo de conteúdo elegível.");
            return errors;
        }

        var admitsMovies = contentTypes.Contains(EContentType.MOVIE);
        var admitsSeries = contentTypes.Contains(EContentType.SERIES);

        for (var index = 0; index < rules.Count; index++)
        {
            var rule = rules[index];
            var position = index + 1;

            var property = CurationFilterCatalog.Find(rule.Property);

            if (property is null)
            {
                errors.Add($"Filtro {position}: a propriedade '{rule.Property}' não é permitida.");
                continue;
            }

            if ((admitsMovies && !property.SupportsMovies) ||
                (admitsSeries && !property.SupportsSeries))
            {
                errors.Add($"Filtro {position}: a propriedade '{property.Label}' não é compatível com os tipos de conteúdo da coleção.");
            }

            if (!CurationFilterCatalog.SupportsOperator(property.ValueType, rule.Operator))
            {
                errors.Add($"Filtro {position}: o operador '{rule.Operator}' não é válido para '{property.Label}'.");
            }

            if (string.IsNullOrWhiteSpace(rule.Value))
            {
                errors.Add($"Filtro {position}: informe o valor de '{property.Label}'.");
                continue;
            }

            if (rule.Operator == ECurationFilterOperator.BETWEEN &&
                string.IsNullOrWhiteSpace(rule.SecondValue))
            {
                errors.Add($"Filtro {position}: informe o segundo valor para o operador BETWEEN em '{property.Label}'.");
            }

            if (property.ValueType == ECurationFilterValueType.NUMBER &&
                !double.TryParse(rule.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                errors.Add($"Filtro {position}: o valor de '{property.Label}' deve ser numérico.");
            }

            if (property.ValueType == ECurationFilterValueType.DATE &&
                !DateTime.TryParse(rule.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                errors.Add($"Filtro {position}: o valor de '{property.Label}' deve ser uma data válida.");
            }

            if (rule.Operator == ECurationFilterOperator.BETWEEN &&
                !string.IsNullOrWhiteSpace(rule.SecondValue) &&
                property.ValueType == ECurationFilterValueType.NUMBER &&
                !double.TryParse(rule.SecondValue, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                errors.Add($"Filtro {position}: o segundo valor de '{property.Label}' deve ser numérico.");
            }

            if (rule.Operator == ECurationFilterOperator.BETWEEN &&
                !string.IsNullOrWhiteSpace(rule.SecondValue) &&
                property.ValueType == ECurationFilterValueType.DATE &&
                !DateTime.TryParse(rule.SecondValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                errors.Add($"Filtro {position}: o segundo valor de '{property.Label}' deve ser uma data válida.");
            }
        }

        return errors;
    }

    public static void NormalizeValueTypes(IEnumerable<DynamicFilterRule> rules)
    {
        foreach (var rule in rules)
        {
            var property = CurationFilterCatalog.Find(rule.Property);
            if (property is not null)
                rule.ValueType = property.ValueType;
        }
    }
}
