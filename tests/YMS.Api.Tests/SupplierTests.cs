using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using YMS.Api.Tests.Support;

namespace YMS.Api.Tests;

public sealed class SupplierTests(YmsApiFactory factory) : IClassFixture<YmsApiFactory>
{
    private const string ValidCnpj = "11222333000181";
    private const string OtherValidCnpj = "11444777000161";

    private static object Supplier(string externalId = "SUP-1", string cnpj = "11.222.333/0001-81", object? address = null) => new
    {
        externalId,
        name = "Distribuidora Exemplo Ltda",
        cnpj,
        address = address ?? BrazilianAddress(),
    };

    private static object BrazilianAddress(string state = "SP", string zipCode = "01310-100") => new
    {
        country = "BR", state, city = "São Paulo", street = "Avenida Paulista", number = "1000",
        complement = "Sala 1", district = "Bela Vista", zipCode,
    };

    [Fact]
    public async Task Creating_a_supplier_with_a_masked_cnpj_stores_only_the_digits()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsJsonAsync("/api/v1/suppliers", Supplier());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/v1/suppliers/SUP-1", response.Headers.Location?.ToString());
        var supplier = await erp.GetFromJsonAsync<JsonElement>("/api/v1/suppliers/SUP-1");
        Assert.Equal("Distribuidora Exemplo Ltda", supplier.GetProperty("name").GetString());
        Assert.Equal(ValidCnpj, supplier.GetProperty("cnpj").GetString());
        var address = supplier.GetProperty("address");
        Assert.Equal("BR", address.GetProperty("country").GetString());
        Assert.Equal("SP", address.GetProperty("state").GetString());
        Assert.Equal("Bela Vista", address.GetProperty("district").GetString());
        Assert.Equal("01310100", address.GetProperty("zipCode").GetString());
        Assert.False(supplier.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task A_cnpj_can_be_sent_without_mask()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsJsonAsync("/api/v1/suppliers", Supplier(cnpj: ValidCnpj));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("11.222.333/0001-82")] // wrong check digit
    [InlineData("11111111111111")]     // repeated digits
    [InlineData("1122233300018")]      // 13 digits
    [InlineData("11.222.333/0001-8A")] // not a digit
    public async Task An_invalid_cnpj_is_rejected(string cnpj)
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsJsonAsync("/api/v1/suppliers", Supplier(cnpj: cnpj));

        Expect.Error(await Expect.RejectedAsync(response), "cnpj", "supplier.cnpj_invalid");
    }

    [Fact]
    public async Task Two_suppliers_of_the_same_owner_may_share_a_cnpj_with_different_external_ids()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var first = await erp.PostAsJsonAsync("/api/v1/suppliers", Supplier("SUP-1"));
        var second = await erp.PostAsJsonAsync("/api/v1/suppliers", Supplier("SUP-2"));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task The_same_cnpj_sent_by_two_owners_becomes_two_independent_suppliers()
    {
        var ownerA = await factory.CreateOwnerClientAsync("Owner A");
        var ownerB = await factory.CreateOwnerClientAsync("Owner B");
        await ownerA.PostAsJsonAsync("/api/v1/suppliers", Supplier("SUP-1"));

        var response = await ownerB.PostAsJsonAsync("/api/v1/suppliers", new
        {
            externalId = "SUP-1", name = "Nome no Owner B", cnpj = ValidCnpj, address = BrazilianAddress(),
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Distribuidora Exemplo Ltda", (await ownerA.GetFromJsonAsync<JsonElement>("/api/v1/suppliers/SUP-1")).GetProperty("name").GetString());
        Assert.Equal("Nome no Owner B", (await ownerB.GetFromJsonAsync<JsonElement>("/api/v1/suppliers/SUP-1")).GetProperty("name").GetString());
    }

    [Fact]
    public async Task A_supplier_missing_required_address_fields_is_rejected()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsJsonAsync("/api/v1/suppliers", Supplier(address: new { number = "10" }));

        var problem = await Expect.RejectedAsync(response);
        Expect.Error(problem, "address.country", "supplier.address_country_required");
        Expect.Error(problem, "address.state", "supplier.address_state_required");
        Expect.Error(problem, "address.city", "supplier.address_city_required");
        Expect.Error(problem, "address.street", "supplier.address_street_required");
        Expect.Error(problem, "address.zipCode", "supplier.address_zip_code_required");
    }

    [Fact]
    public async Task A_supplier_without_address_is_rejected()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsJsonAsync("/api/v1/suppliers", new { externalId = "SUP-1", name = "X", cnpj = ValidCnpj });

        Expect.Error(await Expect.RejectedAsync(response), "address", "supplier.address_required");
    }

    [Fact]
    public async Task A_brazilian_address_with_an_unknown_state_or_a_zip_code_without_8_digits_is_rejected()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsJsonAsync("/api/v1/suppliers", Supplier(address: BrazilianAddress(state: "XX", zipCode: "0131-010")));

        var problem = await Expect.RejectedAsync(response);
        Expect.Error(problem, "address.state", "supplier.address_state_invalid");
        Expect.Error(problem, "address.zipCode", "supplier.address_zip_code_invalid");
    }

    [Fact]
    public async Task A_foreign_address_is_not_checked_against_brazilian_states_and_zip_codes()
    {
        var erp = await factory.CreateOwnerClientAsync();

        var response = await erp.PostAsJsonAsync("/api/v1/suppliers", Supplier(address: new
        {
            country = "AR", state = "Buenos Aires", city = "CABA", street = "Av. Corrientes", zipCode = "C1043",
        }));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Resending_a_supplier_updates_it_and_returns_200()
    {
        var erp = await factory.CreateOwnerClientAsync();
        await erp.PostAsJsonAsync("/api/v1/suppliers", Supplier());

        var response = await erp.PostAsJsonAsync("/api/v1/suppliers", new
        {
            externalId = "SUP-1", name = "Novo Nome", cnpj = OtherValidCnpj, address = BrazilianAddress(state: "RJ", zipCode: "20040020"),
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var supplier = await erp.GetFromJsonAsync<JsonElement>("/api/v1/suppliers/SUP-1");
        Assert.Equal("Novo Nome", supplier.GetProperty("name").GetString());
        Assert.Equal(OtherValidCnpj, supplier.GetProperty("cnpj").GetString());
        Assert.Equal("RJ", supplier.GetProperty("address").GetProperty("state").GetString());
    }

    [Fact]
    public async Task An_owner_gets_404_for_a_supplier_of_another_owner()
    {
        var ownerA = await factory.CreateOwnerClientAsync("Owner A");
        var ownerB = await factory.CreateOwnerClientAsync("Owner B");
        await ownerA.PostAsJsonAsync("/api/v1/suppliers", Supplier("SUP-A"));

        Assert.Equal(HttpStatusCode.NotFound, (await ownerB.GetAsync("/api/v1/suppliers/SUP-A")).StatusCode);
    }
}
