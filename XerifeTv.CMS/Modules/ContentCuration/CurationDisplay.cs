using XerifeTv.CMS.Modules.ContentCuration.Enums;

namespace XerifeTv.CMS.Modules.ContentCuration;

public static class CurationDisplay
{
    public static string ContentType(EContentType contentType) =>
        contentType == EContentType.MOVIE ? "Filme" : "Série";

    public static string Mode(ECurationCollectionMode mode) =>
        mode == ECurationCollectionMode.STATIC ? "Estática" : "Dinâmica";

    public static string OrderBy(ECurationOrderBy orderBy) => orderBy switch
    {
        ECurationOrderBy.TITLE => "Título",
        ECurationOrderBy.RELEASE_YEAR => "Ano de lançamento",
        ECurationOrderBy.REVIEW => "Avaliação",
        _ => "Data de criação"
    };

    public static string SortDirection(ECurationSortDirection direction) =>
        direction == ECurationSortDirection.ASC ? "Ascendente" : "Descendente";

    public static string Operator(ECurationFilterOperator op) => op switch
    {
        ECurationFilterOperator.EQUALS => "Igual a",
        ECurationFilterOperator.NOT_EQUALS => "Diferente de",
        ECurationFilterOperator.CONTAINS => "Contém",
        ECurationFilterOperator.NOT_CONTAINS => "Não contém",
        ECurationFilterOperator.GREATER_THAN => "Maior que",
        ECurationFilterOperator.LESS_THAN => "Menor que",
        ECurationFilterOperator.GREATER_THAN_OR_EQUAL => "Maior ou igual a",
        ECurationFilterOperator.LESS_THAN_OR_EQUAL => "Menor ou igual a",
        ECurationFilterOperator.BETWEEN => "Entre",
        _ => op.ToString()
    };

    public static string ValueType(ECurationFilterValueType valueType) => valueType switch
    {
        ECurationFilterValueType.NUMBER => "Número",
        ECurationFilterValueType.DATE => "Data",
        ECurationFilterValueType.LIST => "Lista",
        ECurationFilterValueType.REFERENCE => "Referência",
        _ => "Texto"
    };
}
