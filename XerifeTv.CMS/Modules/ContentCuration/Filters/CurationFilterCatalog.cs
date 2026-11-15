using XerifeTv.CMS.Modules.ContentCuration.Enums;

namespace XerifeTv.CMS.Modules.ContentCuration.Filters;

public sealed record CurationFilterProperty(
    string Name,
    string Label,
    ECurationFilterValueType ValueType,
    bool SupportsMovies,
    bool SupportsSeries,
    string? MovieField,
    string? SeriesField)
{
    public bool IsCommonToAllTypes => SupportsMovies && SupportsSeries;

    public string? FieldFor(EContentType contentType) =>
        contentType == EContentType.MOVIE ? MovieField : SeriesField;
}

public static class CurationFilterCatalog
{
    public static readonly IReadOnlyList<CurationFilterProperty> Properties =
    [
        new("title", "Título", ECurationFilterValueType.TEXT, true, true, "Title", "Title"),
        new("releaseYear", "Ano de lançamento", ECurationFilterValueType.NUMBER, true, true, "ReleaseYear", "ReleaseYear"),
        new("categories", "Categorias", ECurationFilterValueType.LIST, true, true, "Categories", "Categories"),
        new("franchiseId", "Franquia", ECurationFilterValueType.REFERENCE, true, true, "FranchiseId", "FranchiseId"),
        new("review", "Avaliação", ECurationFilterValueType.NUMBER, true, true, "Review", "Review"),
        new("createAt", "Data de criação", ECurationFilterValueType.DATE, true, true, "CreateAt", "CreateAt"),
        new("updateAt", "Data de atualização", ECurationFilterValueType.DATE, true, true, "UpdateAt", "UpdateAt"),
        new("parentalRating", "Classificação indicativa", ECurationFilterValueType.NUMBER, true, true, "ParentalRating", "ParentalRating"),
        new("video.duration", "Duração do filme", ECurationFilterValueType.NUMBER, true, false, "Video.Duration", null),
        new("numberSeasons", "Número de temporadas", ECurationFilterValueType.NUMBER, false, true, null, "NumberSeasons")
    ];

    public static CurationFilterProperty? Find(string property) =>
        Properties.FirstOrDefault(p => p.Name.Equals(property, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<ECurationFilterOperator> AllowedOperators(ECurationFilterValueType valueType) =>
        valueType switch
        {
            ECurationFilterValueType.TEXT =>
                [ECurationFilterOperator.EQUALS, ECurationFilterOperator.NOT_EQUALS,
                 ECurationFilterOperator.CONTAINS, ECurationFilterOperator.NOT_CONTAINS],

            ECurationFilterValueType.LIST =>
                [ECurationFilterOperator.CONTAINS, ECurationFilterOperator.NOT_CONTAINS],

            ECurationFilterValueType.REFERENCE =>
                [ECurationFilterOperator.EQUALS, ECurationFilterOperator.NOT_EQUALS],

            ECurationFilterValueType.NUMBER or ECurationFilterValueType.DATE =>
                [ECurationFilterOperator.EQUALS, ECurationFilterOperator.NOT_EQUALS,
                 ECurationFilterOperator.GREATER_THAN, ECurationFilterOperator.LESS_THAN,
                 ECurationFilterOperator.GREATER_THAN_OR_EQUAL, ECurationFilterOperator.LESS_THAN_OR_EQUAL,
                 ECurationFilterOperator.BETWEEN],

            _ => []
        };

    public static bool SupportsOperator(ECurationFilterValueType valueType, ECurationFilterOperator op) =>
        AllowedOperators(valueType).Contains(op);

    public static IReadOnlyList<CurationFilterProperty> ForTypes(IEnumerable<EContentType> contentTypes)
    {
        var types = contentTypes.Distinct().ToList();
        return [.. Properties.Where(p =>
            (!types.Contains(EContentType.MOVIE) || p.SupportsMovies) &&
            (!types.Contains(EContentType.SERIES) || p.SupportsSeries))];
    }
}
