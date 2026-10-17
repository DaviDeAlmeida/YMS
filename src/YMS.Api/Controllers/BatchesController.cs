using MediatR;
using Microsoft.AspNetCore.Mvc;
using YMS.Application.Batches.Queries.GetBatch;

namespace YMS.Api.Controllers;

[Route(Routes.V1 + "/batches")]
public sealed class BatchesController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Consulta o andamento e o resultado de cada item de um Lote.</summary>
    /// <param name="id">batchId devolvido no envio do Lote.</param>
    /// <response code="200">Lote encontrado, com o resultado de cada item.</response>
    /// <response code="404">Nenhum Lote com esse id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<BatchResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        FoundOrNotFound(await Sender.Send(new GetBatchQuery(id), ct));
}
