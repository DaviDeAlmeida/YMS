using YMS.Domain.Suppliers;

namespace YMS.Application.Suppliers;

public sealed record SupplierResponse(
    string ExternalId,
    string Name,
    string Cnpj,
    AddressResponse Address,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static SupplierResponse From(Supplier supplier) => new(
        supplier.ExternalId,
        supplier.Name,
        supplier.Cnpj,
        new AddressResponse(
            supplier.Address.Country,
            supplier.Address.State,
            supplier.Address.City,
            supplier.Address.Street,
            supplier.Address.Number,
            supplier.Address.Complement,
            supplier.Address.District,
            supplier.Address.ZipCode),
        supplier.CreatedAt,
        supplier.UpdatedAt);
}

public sealed record AddressResponse(
    string Country,
    string State,
    string City,
    string Street,
    string? Number,
    string? Complement,
    string? District,
    string ZipCode);
