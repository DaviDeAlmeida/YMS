using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using YMS.Api.Tests.Support;

namespace YMS.Api.Tests;

public sealed class CategoryTests(YmsApiFactory factory) : IClassFixture<YmsApiFactory>
{
    [Fact]
    public async Task Creating_a_category_returns_201_with_location_and_it_can_be_read_back()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsJsonAsync("/api/v1/categories", new { externalId = "CAT-1", description = "Bebidas" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/v1/categories/CAT-1", response.Headers.Location?.ToString());
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("CAT-1", created.GetProperty("externalId").GetString());
        Assert.Equal("Bebidas", created.GetProperty("description").GetString());

        var fetched = await erp.GetFromJsonAsync<JsonElement>("/api/v1/categories/CAT-1");
        Assert.Equal("Bebidas", fetched.GetProperty("description").GetString());
        Assert.False(fetched.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task Resending_a_category_with_the_same_external_id_updates_it_and_returns_200()
    {
        var erp = await factory.CreateOwnerClientAsync();
        var created = await (await erp.PostAsJsonAsync("/api/v1/categories", new { externalId = "CAT-1", description = "Bebidas" }))
            .Content.ReadFromJsonAsync<JsonElement>();

        var response = await erp.PostAsJsonAsync("/api/v1/categories", new { externalId = "CAT-1", description = "Bebidas e sucos" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Bebidas e sucos", updated.GetProperty("description").GetString());
        Assert.Equal(created.GetProperty("createdAt").GetDateTimeOffset(), updated.GetProperty("createdAt").GetDateTimeOffset());
        var fetched = await erp.GetFromJsonAsync<JsonElement>("/api/v1/categories/CAT-1");
        Assert.Equal("Bebidas e sucos", fetched.GetProperty("description").GetString());
    }

    [Fact]
    public async Task A_category_with_empty_or_too_long_fields_is_rejected_with_422_and_errors_per_field()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsJsonAsync("/api/v1/categories", new { externalId = "", description = new string('x', 201) });

        var problem = await Expect.RejectedAsync(response);
        Expect.Error(problem, "externalId", "category.external_id_required");
        Expect.Error(problem, "description", "category.description_too_long");
    }
}
