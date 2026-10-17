using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using YMS.Api.Tests.Support;

namespace YMS.Api.Tests;

public sealed class ContractTests(YmsApiFactory factory) : IClassFixture<YmsApiFactory>
{
    [Fact]
    public async Task The_api_publishes_an_openapi_document_with_the_v1_routes()
    {
        var document = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        var paths = document.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains("/api/v1/categories", paths);
        Assert.Contains("/api/v1/categories/batch", paths);
        Assert.Contains("/api/v1/products/{externalId}", paths);
        Assert.Contains("/api/v1/suppliers/batch", paths);
        Assert.Contains("/api/v1/batches/{id}", paths);
    }

    [Fact]
    public async Task A_malformed_body_is_answered_with_problem_details()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsync("/api/v1/categories", new StringContent("{ not json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
