using MediatR;
using YMS.Application.Common;

namespace YMS.Application.Suppliers.Commands.UpsertSupplier;

/// <summary>Fornecedor enviado pelo ERP; cria quando o externalId é novo, atualiza quando já existe.</summary>
/// <param name="ExternalId" example="FORN-001">Identificador do Fornecedor no sistema de origem, único por Owner.</param>
/// <param name="Name" example="Distribuidora Exemplo Ltda">Razão social do Fornecedor.</param>
/// <param name="Cnpj" example="11.222.333/0001-81">CNPJ com ou sem máscara; é armazenado só com os dígitos.</param>
/// <param name="Address">Endereço do Fornecedor.</param>
public sealed record UpsertSupplierCommand(string? ExternalId, string? Name, string? Cnpj, AddressInput? Address)
    : IRequest<UpsertResult<SupplierResponse>>;

/// <summary>Endereço; para <c>country = BR</c>, a UF e o CEP de 8 dígitos são validados.</summary>
/// <param name="Country" example="BR">País, código ISO 3166-1 alfa-2.</param>
/// <param name="State" example="SP">UF (sigla) quando o país é BR.</param>
/// <param name="City" example="São Paulo">Cidade.</param>
/// <param name="Street" example="Av. Paulista">Logradouro.</param>
/// <param name="Number" example="1000">Número.</param>
/// <param name="Complement" example="Sala 12">Complemento.</param>
/// <param name="District" example="Bela Vista">Bairro.</param>
/// <param name="ZipCode" example="01310-100">CEP; com ou sem máscara.</param>
public sealed record AddressInput(
    string? Country,
    string? State,
    string? City,
    string? Street,
    string? Number,
    string? Complement,
    string? District,
    string? ZipCode);
