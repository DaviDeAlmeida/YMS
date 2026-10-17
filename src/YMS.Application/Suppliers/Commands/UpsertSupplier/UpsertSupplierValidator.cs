using FluentValidation;
using YMS.Application.Common;
using YMS.Domain.Suppliers;

namespace YMS.Application.Suppliers.Commands.UpsertSupplier;

public sealed class UpsertSupplierValidator : AbstractValidator<UpsertSupplierCommand>
{
    public UpsertSupplierValidator()
    {
        RuleFor(s => s.ExternalId).RequiredText("supplier.external_id", Limits.ExternalId);
        RuleFor(s => s.Name).RequiredText("supplier.name", Limits.Name);

        RuleFor(s => s.Cnpj)
            .Must(Cnpj.IsValid)
            .WithErrorCode("supplier.cnpj_invalid")
            .WithMessage("O CNPJ informado é inválido.");

        RuleFor(s => s.Address)
            .NotNull()
            .WithErrorCode("supplier.address_required")
            .WithMessage("O endereço é obrigatório.")
            .SetValidator(new AddressInputValidator()!);
    }
}

public sealed class AddressInputValidator : AbstractValidator<AddressInput>
{
    public AddressInputValidator()
    {
        RuleFor(a => a.Country).RequiredText("supplier.address_country", Limits.Country);
        RuleFor(a => a.State).RequiredText("supplier.address_state", Limits.AddressPart);
        RuleFor(a => a.City).RequiredText("supplier.address_city", Limits.AddressPart);
        RuleFor(a => a.Street).RequiredText("supplier.address_street", Limits.Description);
        RuleFor(a => a.Number).OptionalText("supplier.address_number", Limits.AddressNumber);
        RuleFor(a => a.Complement).OptionalText("supplier.address_complement", Limits.AddressPart);
        RuleFor(a => a.District).OptionalText("supplier.address_district", Limits.AddressPart);
        RuleFor(a => a.ZipCode).RequiredText("supplier.address_zip_code", Limits.ZipCode);

        When(a => Address.IsBrazil(a.Country), () =>
        {
            RuleFor(a => a.State)
                .Must(state => Address.BrazilianStates.Contains(state!.Trim()))
                .When(a => !string.IsNullOrWhiteSpace(a.State))
                .WithErrorCode("supplier.address_state_invalid")
                .WithMessage("A UF '{PropertyValue}' não existe.");
            RuleFor(a => a.ZipCode)
                .Must(zip => Address.NormalizeBrazilianZipCode(zip!) is { Length: 8 } digits && digits.All(char.IsAsciiDigit))
                .When(a => !string.IsNullOrWhiteSpace(a.ZipCode))
                .WithErrorCode("supplier.address_zip_code_invalid")
                .WithMessage("O CEP deve ter 8 dígitos.");
        });
    }
}
