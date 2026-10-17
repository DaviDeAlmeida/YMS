namespace YMS.Api.Controllers;

public static class Routes
{
    public const string V1 = "/api/v1";

    public static string Category(string externalId) => $"{V1}/categories/{Uri.EscapeDataString(externalId)}";
    public static string Product(string externalId) => $"{V1}/products/{Uri.EscapeDataString(externalId)}";
    public static string Supplier(string externalId) => $"{V1}/suppliers/{Uri.EscapeDataString(externalId)}";
    public static string Batch(Guid id) => $"{V1}/batches/{id}";
}
