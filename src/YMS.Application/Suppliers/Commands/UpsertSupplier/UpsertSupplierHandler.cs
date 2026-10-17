using MediatR;
using Microsoft.EntityFrameworkCore;
using YMS.Application.Common;
using YMS.Domain.Suppliers;

namespace YMS.Application.Suppliers.Commands.UpsertSupplier;

public sealed class UpsertSupplierHandler(IYmsDbContext db, IOwnerContext owner, TimeProvider clock)
    : IRequestHandler<UpsertSupplierCommand, UpsertResult<SupplierResponse>>
{
    public async Task<UpsertResult<SupplierResponse>> Handle(UpsertSupplierCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var address = ToAddress(command.Address!);
        var supplier = await db.Suppliers.SingleOrDefaultAsync(s => s.ExternalId == command.ExternalId, cancellationToken);

        UpsertOutcome outcome;
        if (supplier is null)
        {
            supplier = Supplier.Create(owner.OwnerId, command.ExternalId!, command.Name!, command.Cnpj!, address, now);
            db.Suppliers.Add(supplier);
            outcome = UpsertOutcome.Created;
        }
        else
        {
            supplier.Update(command.Name!, command.Cnpj!, address, now);
            outcome = UpsertOutcome.Updated;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new UpsertResult<SupplierResponse>(SupplierResponse.From(supplier), outcome);
    }

    private static Address ToAddress(AddressInput input)
    {
        var isBrazil = Address.IsBrazil(input.Country);
        return new Address(
            input.Country!.Trim().ToUpperInvariant(),
            isBrazil ? input.State!.Trim().ToUpperInvariant() : input.State!.Trim(),
            input.City!.Trim(),
            input.Street!.Trim(),
            input.Number?.Trim(),
            input.Complement?.Trim(),
            input.District?.Trim(),
            isBrazil ? Address.NormalizeBrazilianZipCode(input.ZipCode!) : input.ZipCode!.Trim());
    }
}
