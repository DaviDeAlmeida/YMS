using MediatR;
using Microsoft.AspNetCore.Mvc;
using YMS.Application.Suppliers;
using YMS.Application.Suppliers.Commands.UpsertSupplier;
using YMS.Application.Suppliers.Queries.GetSupplier;
using YMS.Domain.Batches;

namespace YMS.Api.Controllers;

[Route(Routes.V1 + "/suppliers")]
public sealed class SuppliersController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Cria ou atualiza um Fornecedor pelo externalId.</summary>
    /// <response code="201">Fornecedor criado.</response>
    /// <response code="200">Fornecedor existente atualizado.</response>
    /// <response code="400">Corpo ausente ou JSON malformado.</response>
    /// <response code="422">Requisição rejeitada; os erros vêm por campo.</response>
    [HttpPost]
    [ProducesResponseType<SupplierResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<SupplierResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<RejectionProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<RejectionProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> Upsert(UpsertSupplierCommand command, CancellationToken ct) =>
        Upserted(await Sender.Send(command, ct), s => Routes.Supplier(s.ExternalId));

    /// <summary>Consulta um Fornecedor pelo externalId.</summary>
    /// <param name="externalId">Identificador do Fornecedor no sistema de origem.</param>
    /// <response code="200">Fornecedor encontrado.</response>
    /// <response code="404">Nenhum Fornecedor com esse externalId.</response>
    [HttpGet("{externalId}")]
    [ProducesResponseType<SupplierResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Get(string externalId, CancellationToken ct) =>
        FoundOrNotFound(await Sender.Send(new GetSupplierQuery(externalId), ct));

    /// <summary>Envia um Lote de Fornecedores para processamento assíncrono.</summary>
    /// <response code="202">Lote aceito; acompanhe o resultado pelo <c>Location</c>.</response>
    /// <response code="400">Corpo ausente ou JSON malformado.</response>
    /// <response code="422">Lote rejeitado como um todo (vazio, acima de 500 itens ou malformado).</response>
    [HttpPost("batch")]
    [ProducesResponseType<BatchAccepted>(StatusCodes.Status202Accepted, "application/json")]
    [ProducesResponseType<RejectionProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<RejectionProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public Task<IActionResult> Batch(BatchRequest request, CancellationToken ct) =>
        SubmitBatch(BatchType.Suppliers, request, ct);
}
