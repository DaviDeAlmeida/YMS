using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using YMS.Api.Tests.Support;

namespace YMS.Api.Tests;

public sealed class BatchTests(YmsApiFactory factory) : IClassFixture<YmsApiFactory>
{
    [Fact]
    public async Task A_category_batch_is_accepted_with_202_and_its_items_are_created()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsJsonAsync("/api/v1/categories/batch", new
        {
            items = new[] { new { externalId = "CAT-1", description = "Bebidas" }, new { externalId = "CAT-2", description = "Limpeza" } },
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var batchId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("batchId").GetGuid();
        Assert.Equal($"/api/v1/batches/{batchId}", response.Headers.Location?.ToString());

        var batch = await BatchPolling.WaitUntilCompletedAsync(erp, response.Headers.Location!);
        Assert.Equal("categories", batch.GetProperty("type").GetString());
        Assert.NotEqual(JsonValueKind.Null, batch.GetProperty("receivedAt").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, batch.GetProperty("completedAt").ValueKind);
        Assert.Equal("created", BatchPolling.Item(batch, 0).GetProperty("result").GetString());
        Assert.Equal("CAT-1", BatchPolling.Item(batch, 0).GetProperty("externalId").GetString());
        Assert.Equal("created", BatchPolling.Item(batch, 1).GetProperty("result").GetString());
        Assert.Equal("Limpeza", (await erp.GetFromJsonAsync<JsonElement>("/api/v1/categories/CAT-2")).GetProperty("description").GetString());
    }

    [Fact]
    public async Task Valid_items_are_saved_even_when_others_are_rejected_with_the_unit_endpoint_errors()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsJsonAsync("/api/v1/categories/batch", new
        {
            items = new[] { new { externalId = "CAT-1", description = "Bebidas" }, new { externalId = "CAT-2", description = "" } },
        });

        var batch = await BatchPolling.WaitUntilCompletedAsync(erp, response.Headers.Location!);
        Assert.Equal("created", BatchPolling.Item(batch, 0).GetProperty("result").GetString());
        var rejected = BatchPolling.Item(batch, 1);
        Assert.Equal("rejected", rejected.GetProperty("result").GetString());
        Assert.Equal("CAT-2", rejected.GetProperty("externalId").GetString());
        Expect.Error(rejected, "description", "category.description_required");
        Assert.Equal(HttpStatusCode.OK, (await erp.GetAsync("/api/v1/categories/CAT-1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await erp.GetAsync("/api/v1/categories/CAT-2")).StatusCode);
    }

    [Fact]
    public async Task Items_are_processed_in_the_order_sent()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsJsonAsync("/api/v1/categories/batch", new
        {
            items = new[] { new { externalId = "CAT-1", description = "Primeira" }, new { externalId = "CAT-1", description = "Segunda" } },
        });

        var batch = await BatchPolling.WaitUntilCompletedAsync(erp, response.Headers.Location!);
        Assert.Equal("created", BatchPolling.Item(batch, 0).GetProperty("result").GetString());
        Assert.Equal("updated", BatchPolling.Item(batch, 1).GetProperty("result").GetString());
        Assert.Equal("Segunda", (await erp.GetFromJsonAsync<JsonElement>("/api/v1/categories/CAT-1")).GetProperty("description").GetString());
    }

    [Fact]
    public async Task A_product_batch_rejects_items_whose_category_does_not_exist()
    {
        var erp = await factory.CreateOwnerClientAsync();
        await erp.PostAsJsonAsync("/api/v1/categories", new { externalId = "CAT-1", description = "Bebidas" });

        var response = await erp.PostAsJsonAsync("/api/v1/products/batch", new
        {
            items = new object[]
            {
                new { externalId = "PRD-1", description = "Refrigerante", categoryExternalId = "CAT-1", eans = new[] { new { value = "1" } } },
                new { externalId = "PRD-2", description = "Detergente", categoryExternalId = "CAT-404", eans = new[] { new { value = "2" } } },
            },
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var batch = await BatchPolling.WaitUntilCompletedAsync(erp, response.Headers.Location!);
        Assert.Equal("products", batch.GetProperty("type").GetString());
        Assert.Equal("created", BatchPolling.Item(batch, 0).GetProperty("result").GetString());
        Expect.Error(BatchPolling.Item(batch, 1), "categoryExternalId", "category.not_found");
        Assert.Equal("UN", (await erp.GetFromJsonAsync<JsonElement>("/api/v1/products/PRD-1")).GetProperty("unitOfMeasure").GetString());
    }

    [Fact]
    public async Task A_supplier_batch_creates_and_rejects_item_by_item()
    {
        var erp = await factory.CreateOwnerClientAsync();
        var address = new { country = "BR", state = "SP", city = "São Paulo", street = "Av. Paulista", zipCode = "01310100" };

        var response = await erp.PostAsJsonAsync("/api/v1/suppliers/batch", new
        {
            items = new[]
            {
                new { externalId = "SUP-1", name = "Fornecedor 1", cnpj = "11222333000181", address },
                new { externalId = "SUP-2", name = "Fornecedor 2", cnpj = "11222333000182", address },
            },
        });

        var batch = await BatchPolling.WaitUntilCompletedAsync(erp, response.Headers.Location!);
        Assert.Equal("suppliers", batch.GetProperty("type").GetString());
        Assert.Equal("created", BatchPolling.Item(batch, 0).GetProperty("result").GetString());
        Expect.Error(BatchPolling.Item(batch, 1), "cnpj", "supplier.cnpj_invalid");
    }

    [Fact]
    public async Task An_item_that_is_not_an_object_of_the_batch_type_is_rejected()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsJsonAsync("/api/v1/categories/batch", new
        {
            items = new object[] { "not-an-object", new { externalId = "CAT-1", description = "Bebidas" } },
        });

        var batch = await BatchPolling.WaitUntilCompletedAsync(erp, response.Headers.Location!);
        var rejected = BatchPolling.Item(batch, 0);
        Assert.Equal("rejected", rejected.GetProperty("result").GetString());
        Expect.Error(rejected, "", "batch.item_invalid");
        Assert.Equal("created", BatchPolling.Item(batch, 1).GetProperty("result").GetString());
    }

    [Fact]
    public async Task An_item_with_an_oversized_external_id_is_rejected_without_failing_the_batch()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsJsonAsync("/api/v1/categories/batch", new
        {
            items = new[] { new { externalId = new string('X', 51), description = "Longa" }, new { externalId = "CAT-1", description = "Bebidas" } },
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var batch = await BatchPolling.WaitUntilCompletedAsync(erp, response.Headers.Location!);
        Expect.Error(BatchPolling.Item(batch, 0), "externalId", "category.external_id_too_long");
        Assert.Equal("created", BatchPolling.Item(batch, 1).GetProperty("result").GetString());
    }

    [Fact]
    public async Task An_empty_batch_is_rejected_immediately()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsJsonAsync("/api/v1/categories/batch", new { items = Array.Empty<object>() });

        Expect.Error(await Expect.RejectedAsync(response), "items", "batch.items_required");
    }

    [Fact]
    public async Task A_batch_with_more_than_500_items_is_rejected_immediately_and_nothing_is_processed()
    {
        var erp = await factory.CreateOwnerClientAsync();
        var items = Enumerable.Range(1, 501).Select(i => new { externalId = $"CAT-{i}", description = "X" }).ToArray();

        var response = await erp.PostAsJsonAsync("/api/v1/categories/batch", new { items });

        Expect.Error(await Expect.RejectedAsync(response), "items", "batch.too_many_items");
        Assert.Equal(HttpStatusCode.NotFound, (await erp.GetAsync("/api/v1/categories/CAT-1")).StatusCode);
    }

    [Fact]
    public async Task A_batch_with_exactly_500_items_is_accepted()
    {
        var erp = await factory.CreateOwnerClientAsync();
        var items = Enumerable.Range(1, 500).Select(i => new { externalId = $"CAT-{i}", description = "X" }).ToArray();

        var response = await erp.PostAsJsonAsync("/api/v1/categories/batch", new { items });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var batch = await BatchPolling.WaitUntilCompletedAsync(erp, response.Headers.Location!, TimeSpan.FromSeconds(30));
        Assert.All(batch.GetProperty("items").EnumerateArray(), item => Assert.Equal("created", item.GetProperty("result").GetString()));
    }

    [Fact]
    public async Task An_owner_gets_404_for_a_batch_of_another_owner()
    {
        var ownerA = await factory.CreateOwnerClientAsync("Owner A");
        var ownerB = await factory.CreateOwnerClientAsync("Owner B");
        var response = await ownerA.PostAsJsonAsync("/api/v1/categories/batch", new { items = new[] { new { externalId = "CAT-1", description = "X" } } });

        Assert.Equal(HttpStatusCode.NotFound, (await ownerB.GetAsync(response.Headers.Location)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ownerB.GetAsync("/api/v1/categories/CAT-1")).StatusCode);
    }

    [Fact]
    public async Task Items_of_a_batch_are_written_to_the_owner_that_sent_it()
    {
        var ownerA = await factory.CreateOwnerClientAsync("Owner A");
        var ownerB = await factory.CreateOwnerClientAsync("Owner B");
        await ownerB.PostAsJsonAsync("/api/v1/categories", new { externalId = "CAT-1", description = "Do Owner B" });

        var response = await ownerA.PostAsJsonAsync("/api/v1/categories/batch", new { items = new[] { new { externalId = "CAT-1", description = "Do Owner A" } } });

        var batch = await BatchPolling.WaitUntilCompletedAsync(ownerA, response.Headers.Location!);
        Assert.Equal("created", BatchPolling.Item(batch, 0).GetProperty("result").GetString());
        Assert.Equal("Do Owner B", (await ownerB.GetFromJsonAsync<JsonElement>("/api/v1/categories/CAT-1")).GetProperty("description").GetString());
    }
}
