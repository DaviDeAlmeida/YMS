using MediatR;
using YMS.Application.Common;

namespace YMS.Application.Categories.Commands.UpsertCategory;

/// <summary>Categoria enviada pelo ERP; cria quando o externalId é novo, atualiza quando já existe.</summary>
/// <param name="ExternalId" example="CAT-001">Identificador da Categoria no sistema de origem, único por Owner.</param>
/// <param name="Description" example="Bebidas">Descrição da Categoria.</param>
public sealed record UpsertCategoryCommand(string? ExternalId, string? Description)
    : IRequest<UpsertResult<CategoryResponse>>;
