using MediatR;
using YMS.Application.Common;

namespace YMS.Application.Products.Commands.UpsertProduct;

/// <summary>Produto enviado pelo ERP; cria quando o externalId é novo, atualiza quando já existe.</summary>
/// <param name="ExternalId" example="PROD-001">Identificador do Produto no sistema de origem, único por Owner.</param>
/// <param name="Description" example="Refrigerante lata 350ml">Descrição do Produto.</param>
/// <param name="CategoryExternalId" example="CAT-001">externalId de uma Categoria já cadastrada pelo mesmo Owner.</param>
/// <param name="Eans">Códigos EAN do Produto; exatamente um deve ser o principal.</param>
public sealed record UpsertProductCommand(
    string? ExternalId,
    string? Description,
    string? CategoryExternalId,
    IReadOnlyList<EanInput?>? Eans)
    : IRequest<UpsertResult<ProductResponse>>;

/// <summary>Código EAN do Produto.</summary>
/// <param name="Value" example="7891234567895">EAN-8, EAN-13 ou EAN-14, com dígito verificador válido.</param>
/// <param name="IsPrimary" example="true">Indica o EAN principal do Produto.</param>
public sealed record EanInput(string? Value, bool? IsPrimary);
