using MediatR;
using YMS.Application.Common;

namespace YMS.Application.Suppliers.Commands.UpsertSupplier;

public sealed record UpsertSupplierCommand(string? ExternalId, string? Name, string? Cnpj, AddressInput? Address)
    : IRequest<UpsertResult<SupplierResponse>>;

public sealed record AddressInput(
    string? Country,
    string? State,
    string? City,
    string? Street,
    string? Number,
    string? Complement,
    string? District,
    string? ZipCode);
