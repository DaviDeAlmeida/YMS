using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using YMS.Api.Tests.Support;

namespace YMS.Api.Tests;

public sealed class OwnerIsolationTests(YmsApiFactory factory) : IClassFixture<YmsApiFactory>
{
    [Fact]
    public async Task Two_owners_can_use_the_same_category_external_id_without_conflict()
    {
        var ownerA = await factory.CreateOwnerClientAsync("Owner A");
        var ownerB = await factory.CreateOwnerClientAsync("Owner B");

        var createdByA = await ownerA.PostAsJsonAsync("/api/v1/categories", new { externalId = "CAT-1", description = "Bebidas" });
        var createdByB = await ownerB.PostAsJsonAsync("/api/v1/categories", new { externalId = "CAT-1", description = "Limpeza" });

        Assert.Equal(HttpStatusCode.Created, createdByA.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createdByB.StatusCode);
        Assert.Equal("Bebidas", (await ownerA.GetFromJsonAsync<JsonElement>("/api/v1/categories/CAT-1")).GetProperty("description").GetString());
        Assert.Equal("Limpeza", (await ownerB.GetFromJsonAsync<JsonElement>("/api/v1/categories/CAT-1")).GetProperty("description").GetString());
    }

    [Fact]
    public async Task An_owner_gets_404_for_a_category_of_another_owner()
    {
        var ownerA = await factory.CreateOwnerClientAsync("Owner A");
        var ownerB = await factory.CreateOwnerClientAsync("Owner B");
        await ownerA.PostAsJsonAsync("/api/v1/categories", new { externalId = "ONLY-A", description = "Bebidas" });

        var response = await ownerB.GetAsync("/api/v1/categories/ONLY-A");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task A_request_without_an_authenticated_owner_is_refused()
    {
        var anonymous = factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/api/v1/categories", new { externalId = "CAT-1", description = "Bebidas" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
