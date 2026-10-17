using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using YMS.Api.Tests.Support;

namespace YMS.Api.Tests;

public sealed class ProductTests(YmsApiFactory factory) : IClassFixture<YmsApiFactory>
{
    private async Task<HttpClient> OwnerWithCategoriesAsync(params string[] categoryExternalIds)
    {
        var erp = await factory.CreateOwnerClientAsync();
        foreach (var externalId in categoryExternalIds)
        {
            var response = await erp.PostAsJsonAsync("/api/v1/categories", new { externalId, description = $"Categoria {externalId}" });
            response.EnsureSuccessStatusCode();
        }
        return erp;
    }

    [Fact]
    public async Task Creating_a_product_returns_201_with_its_category_eans_and_unit_UN()
    {
        var erp = await OwnerWithCategoriesAsync("CAT-1");

        var response = await erp.PostAsJsonAsync("/api/v1/products", new
        {
            externalId = "PRD-1",
            description = "Refrigerante 2L",
            categoryExternalId = "CAT-1",
            eans = new[] { new { value = "7891000100103", isPrimary = true }, new { value = "17891000100100", isPrimary = false } },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/v1/products/PRD-1", response.Headers.Location?.ToString());
        var product = await erp.GetFromJsonAsync<JsonElement>("/api/v1/products/PRD-1");
        Assert.Equal("Refrigerante 2L", product.GetProperty("description").GetString());
        Assert.Equal("CAT-1", product.GetProperty("categoryExternalId").GetString());
        Assert.Equal("UN", product.GetProperty("unitOfMeasure").GetString());
        var eans = product.GetProperty("eans").EnumerateArray().ToList();
        Assert.Equal(2, eans.Count);
        Assert.Equal("7891000100103", eans[0].GetProperty("value").GetString());
        Assert.True(eans[0].GetProperty("isPrimary").GetBoolean());
        Assert.False(eans[1].GetProperty("isPrimary").GetBoolean());
        Assert.False(product.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task A_product_whose_category_does_not_exist_in_the_owner_is_rejected()
    {
        await OwnerWithCategoriesAsync("CAT-OTHER");
        var erp = await OwnerWithCategoriesAsync();

        var response = await erp.PostAsJsonAsync("/api/v1/products", new
        {
            externalId = "PRD-1", description = "Refrigerante 2L", categoryExternalId = "CAT-OTHER",
            eans = new[] { new { value = "789", isPrimary = true } },
        });

        var problem = await Expect.RejectedAsync(response);
        Expect.Error(problem, "categoryExternalId", "category.not_found");
        Assert.Equal(HttpStatusCode.NotFound, (await erp.GetAsync("/api/v1/products/PRD-1")).StatusCode);
    }

    [Fact]
    public async Task A_product_without_eans_is_rejected()
    {
        var erp = await OwnerWithCategoriesAsync("CAT-1");

        var response = await erp.PostAsJsonAsync("/api/v1/products", new
        {
            externalId = "PRD-1", description = "Refrigerante 2L", categoryExternalId = "CAT-1", eans = Array.Empty<object>(),
        });

        Expect.Error(await Expect.RejectedAsync(response), "eans", "product.ean_required");
    }

    [Fact]
    public async Task A_product_with_several_eans_and_no_primary_is_rejected()
    {
        var erp = await OwnerWithCategoriesAsync("CAT-1");

        var response = await erp.PostAsJsonAsync("/api/v1/products", new
        {
            externalId = "PRD-1", description = "Refrigerante 2L", categoryExternalId = "CAT-1",
            eans = new[] { new { value = "111", isPrimary = false }, new { value = "222", isPrimary = false } },
        });

        Expect.Error(await Expect.RejectedAsync(response), "eans", "product.primary_ean_required");
    }

    [Fact]
    public async Task A_product_with_more_than_one_primary_ean_is_rejected()
    {
        var erp = await OwnerWithCategoriesAsync("CAT-1");

        var response = await erp.PostAsJsonAsync("/api/v1/products", new
        {
            externalId = "PRD-1", description = "Refrigerante 2L", categoryExternalId = "CAT-1",
            eans = new[] { new { value = "111", isPrimary = true }, new { value = "222", isPrimary = true } },
        });

        Expect.Error(await Expect.RejectedAsync(response), "eans", "product.single_primary_ean");
    }

    [Fact]
    public async Task An_empty_ean_value_is_rejected_with_its_index_in_the_field_path()
    {
        var erp = await OwnerWithCategoriesAsync("CAT-1");

        var response = await erp.PostAsJsonAsync("/api/v1/products", new
        {
            externalId = "PRD-1", description = "Refrigerante 2L", categoryExternalId = "CAT-1",
            eans = new[] { new { value = "111", isPrimary = true }, new { value = "", isPrimary = false } },
        });

        var error = Expect.Error(await Expect.RejectedAsync(response), "eans[1].value", "product.ean_value_required");
        Assert.Contains("eans[1].value", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task A_single_ean_becomes_primary_even_without_is_primary()
    {
        var erp = await OwnerWithCategoriesAsync("CAT-1");

        var response = await erp.PostAsJsonAsync("/api/v1/products", new
        {
            externalId = "PRD-1", description = "Refrigerante 2L", categoryExternalId = "CAT-1",
            eans = new[] { new { value = "789" } },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ean = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("eans")[0];
        Assert.True(ean.GetProperty("isPrimary").GetBoolean());
    }

    [Fact]
    public async Task Eans_are_free_text_and_may_repeat_across_products()
    {
        var erp = await OwnerWithCategoriesAsync("CAT-1");
        var ean = new[] { new { value = "abc-não-é-gtin", isPrimary = true } };

        var first = await erp.PostAsJsonAsync("/api/v1/products", new { externalId = "PRD-1", description = "A", categoryExternalId = "CAT-1", eans = ean });
        var second = await erp.PostAsJsonAsync("/api/v1/products", new { externalId = "PRD-2", description = "B", categoryExternalId = "CAT-1", eans = ean });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task Resending_a_product_updates_description_and_category_and_replaces_the_eans()
    {
        var erp = await OwnerWithCategoriesAsync("CAT-1", "CAT-2");
        await erp.PostAsJsonAsync("/api/v1/products", new
        {
            externalId = "PRD-1", description = "Refrigerante 2L", categoryExternalId = "CAT-1",
            eans = new[] { new { value = "111", isPrimary = true }, new { value = "222", isPrimary = false } },
        });

        var response = await erp.PostAsJsonAsync("/api/v1/products", new
        {
            externalId = "PRD-1", description = "Refrigerante 2,5L", categoryExternalId = "CAT-2",
            eans = new[] { new { value = "333", isPrimary = true } },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var product = await erp.GetFromJsonAsync<JsonElement>("/api/v1/products/PRD-1");
        Assert.Equal("Refrigerante 2,5L", product.GetProperty("description").GetString());
        Assert.Equal("CAT-2", product.GetProperty("categoryExternalId").GetString());
        var eans = product.GetProperty("eans").EnumerateArray().Select(e => e.GetProperty("value").GetString()).ToList();
        Assert.Equal(["333"], eans);
    }

    [Fact]
    public async Task An_owner_gets_404_for_a_product_of_another_owner()
    {
        var ownerA = await OwnerWithCategoriesAsync("CAT-1");
        var ownerB = await factory.CreateOwnerClientAsync();
        await ownerA.PostAsJsonAsync("/api/v1/products", new
        {
            externalId = "PRD-A", description = "A", categoryExternalId = "CAT-1", eans = new[] { new { value = "1", isPrimary = true } },
        });

        Assert.Equal(HttpStatusCode.NotFound, (await ownerB.GetAsync("/api/v1/products/PRD-A")).StatusCode);
    }
}
