using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using YMS.Application.Common;

namespace YMS.Api.Controllers;

/// <summary>Lote de registros para processamento assíncrono.</summary>
/// <param name="Items">Até 500 itens, cada um no mesmo formato do endpoint unitário correspondente.</param>
public sealed record BatchRequest(IReadOnlyList<JsonElement>? Items);

/// <summary>Lote aceito para processamento.</summary>
/// <param name="BatchId">Identificador do Lote, usado para consultar o resultado em <c>GET /api/v1/batches/{id}</c>.</param>
public sealed record BatchAccepted(Guid BatchId);

/// <summary>
/// Problem Details (RFC 9457) de uma requisição rejeitada. Documenta o formato de <c>errors</c>; o corpo é escrito por
/// <see cref="Errors.RejectionExceptionHandler"/>.
/// </summary>
public sealed class RejectionProblemDetails : ProblemDetails
{
    /// <summary>Erros por campo (caminho camelCase, ex.: <c>eans[1].value</c>), cada um com <c>code</c> estável e <c>message</c>.</summary>
    public Dictionary<string, ErrorEntry[]> Errors { get; init; } = [];
}
