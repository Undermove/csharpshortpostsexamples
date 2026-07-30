namespace HttpQueryExample;

// MapQuery() вырезали из .NET 10 на API-ревью — вот те самые «три строки», которыми живём до .NET 11
public static class QueryEndpointExtensions
{
    public static IEndpointConventionBuilder MapQuery(
        this IEndpointRouteBuilder endpoints, string pattern, Delegate handler) =>
        endpoints.MapMethods(pattern, [HttpMethods.Query], handler);
}
