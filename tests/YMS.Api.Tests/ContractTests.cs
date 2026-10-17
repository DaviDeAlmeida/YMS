using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using YMS.Api.Tests.Support;

namespace YMS.Api.Tests;

public sealed class ContractTests(YmsApiFactory factory) : IClassFixture<YmsApiFactory>
{
    [Fact]
    public async Task The_swagger_document_lists_the_v1_routes()
    {
        var document = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");

        var paths = document.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains("/api/v1/categories", paths);
        Assert.Contains("/api/v1/categories/batch", paths);
        Assert.Contains("/api/v1/categories/{externalId}", paths);
        Assert.Contains("/api/v1/products", paths);
        Assert.Contains("/api/v1/products/batch", paths);
        Assert.Contains("/api/v1/products/{externalId}", paths);
        Assert.Contains("/api/v1/suppliers", paths);
        Assert.Contains("/api/v1/suppliers/batch", paths);
        Assert.Contains("/api/v1/suppliers/{externalId}", paths);
        Assert.Contains("/api/v1/batches/{id}", paths);
    }

    [Fact]
    public async Task Each_endpoint_is_documented_with_a_summary_and_its_responses()
    {
        var document = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");

        var operations = document.GetProperty("paths").EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject().Select(op => (Route: $"{op.Name.ToUpperInvariant()} {path.Name}", Operation: op.Value)))
            .ToList();
        Assert.Equal(10, operations.Count);
        Assert.All(operations, op =>
            Assert.False(string.IsNullOrWhiteSpace(op.Operation.TryGetProperty("summary", out var s) ? s.GetString() : null), $"{op.Route} has no summary"));

        var upsert = operations.Single(op => op.Route == "POST /api/v1/categories").Operation;
        var responses = upsert.GetProperty("responses").EnumerateObject().Select(r => r.Name).ToList();
        Assert.Equal(["200", "201", "400", "422"], responses.Order().ToList());
        Assert.False(string.IsNullOrWhiteSpace(upsert.GetProperty("responses").GetProperty("422").GetProperty("description").GetString()));
    }

    [Fact]
    public async Task The_contract_models_are_documented()
    {
        var document = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");

        var supplier = document.GetProperty("components").GetProperty("schemas").GetProperty("UpsertSupplierCommand").GetProperty("properties");
        Assert.False(string.IsNullOrWhiteSpace(supplier.GetProperty("cnpj").GetProperty("description").GetString()));
        Assert.Equal("11.222.333/0001-81", supplier.GetProperty("cnpj").GetProperty("example").GetString());
    }

    [Fact]
    public async Task The_swagger_ui_is_served()
    {
        var response = await factory.CreateClient().GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task A_malformed_body_is_answered_with_problem_details()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsync("/api/v1/categories", new StringContent("{ not json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = problem.GetProperty("errors");
        Assert.Equal(["body"], errors.EnumerateObject().Select(e => e.Name));
        Assert.Equal("request.malformed_json", errors.GetProperty("body")[0].GetProperty("code").GetString());
        Assert.Equal("O corpo da requisição não é um JSON válido.", errors.GetProperty("body")[0].GetProperty("message").GetString());
    }

    [Fact]
    public async Task A_value_of_the_wrong_type_is_reported_on_its_field()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsync("/api/v1/products", new StringContent(
            """{ "externalId": "P1", "eans": [{ "value": "7891234567895", "isPrimary": "sim" }] }""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.Equal(["eans[0].isPrimary"], errors.EnumerateObject().Select(e => e.Name));
        Assert.Equal("request.invalid_type", errors.GetProperty("eans[0].isPrimary")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task An_empty_body_is_reported_as_required()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsync("/api/v1/categories", new StringContent("", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.Equal(["body"], errors.EnumerateObject().Select(e => e.Name));
        Assert.Equal("request.body_required", errors.GetProperty("body")[0].GetProperty("code").GetString());
    }
}
