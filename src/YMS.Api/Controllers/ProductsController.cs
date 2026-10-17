using MediatR;
using Microsoft.AspNetCore.Mvc;
using YMS.Application.Products;
using YMS.Application.Products.Commands.UpsertProduct;
using YMS.Application.Products.Queries.GetProduct;
using YMS.Domain.Batches;

namespace YMS.Api.Controllers;

[Route(Routes.V1 + "/products")]
public sealed class ProductsController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Cria ou atualiza um Produto pelo externalId.</summary>
    /// <response code="201">Produto criado.</response>
    /// <response code="200">Produto existente atualizado.</response>
    /// <response code="400">Corpo ausente ou JSON malformado.</response>
    /// <response code="422">Requisição rejeitada; os erros vêm por campo.</response>
    [HttpPost]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<RejectionProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<RejectionProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> Upsert(UpsertProductCommand command, CancellationToken ct) =>
        Upserted(await Sender.Send(command, ct), p => Routes.Product(p.ExternalId));

    /// <summary>Consulta um Produto pelo externalId.</summary>
    /// <param name="externalId">Identificador do Produto no sistema de origem.</param>
    /// <response code="200">Produto encontrado.</response>
    /// <response code="404">Nenhum Produto com esse externalId.</response>
    [HttpGet("{externalId}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Get(string externalId, CancellationToken ct) =>
        FoundOrNotFound(await Sender.Send(new GetProductQuery(externalId), ct));

    /// <summary>Envia um Lote de Produtos para processamento assíncrono.</summary>
    /// <response code="202">Lote aceito; acompanhe o resultado pelo <c>Location</c>.</response>
    /// <response code="400">Corpo ausente ou JSON malformado.</response>
    /// <response code="422">Lote rejeitado como um todo (vazio, acima de 500 itens ou malformado).</response>
    [HttpPost("batch")]
    [ProducesResponseType<BatchAccepted>(StatusCodes.Status202Accepted, "application/json")]
    [ProducesResponseType<RejectionProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<RejectionProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public Task<IActionResult> Batch(BatchRequest request, CancellationToken ct) =>
        SubmitBatch(BatchType.Products, request, ct);
}
