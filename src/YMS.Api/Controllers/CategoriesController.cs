using MediatR;
using Microsoft.AspNetCore.Mvc;
using YMS.Application.Categories;
using YMS.Application.Categories.Commands.UpsertCategory;
using YMS.Application.Categories.Queries.GetCategory;
using YMS.Domain.Batches;

namespace YMS.Api.Controllers;

[Route(Routes.V1 + "/categories")]
public sealed class CategoriesController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Cria ou atualiza uma Categoria pelo externalId.</summary>
    /// <response code="201">Categoria criada.</response>
    /// <response code="200">Categoria existente atualizada.</response>
    /// <response code="400">Corpo ausente ou JSON malformado.</response>
    /// <response code="422">Requisição rejeitada; os erros vêm por campo.</response>
    [HttpPost]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<RejectionProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<RejectionProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> Upsert(UpsertCategoryCommand command, CancellationToken ct) =>
        Upserted(await Sender.Send(command, ct), c => Routes.Category(c.ExternalId));

    /// <summary>Consulta uma Categoria pelo externalId.</summary>
    /// <param name="externalId">Identificador da Categoria no sistema de origem.</param>
    /// <response code="200">Categoria encontrada.</response>
    /// <response code="404">Nenhuma Categoria com esse externalId.</response>
    [HttpGet("{externalId}")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Get(string externalId, CancellationToken ct) =>
        FoundOrNotFound(await Sender.Send(new GetCategoryQuery(externalId), ct));

    /// <summary>Envia um Lote de Categorias para processamento assíncrono.</summary>
    /// <response code="202">Lote aceito; acompanhe o resultado pelo <c>Location</c>.</response>
    /// <response code="400">Corpo ausente ou JSON malformado.</response>
    /// <response code="422">Lote rejeitado como um todo (vazio, acima de 500 itens ou malformado).</response>
    [HttpPost("batch")]
    [ProducesResponseType<BatchAccepted>(StatusCodes.Status202Accepted, "application/json")]
    [ProducesResponseType<RejectionProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<RejectionProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public Task<IActionResult> Batch(BatchRequest request, CancellationToken ct) =>
        SubmitBatch(BatchType.Categories, request, ct);
}
