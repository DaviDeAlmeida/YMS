using MediatR;
using Microsoft.EntityFrameworkCore;
using YMS.Application.Common;

namespace YMS.Application.Batches.Queries.GetBatch;

public sealed record GetBatchQuery(Guid Id) : IRequest<BatchResponse?>;

public sealed class GetBatchHandler(IYmsDbContext db) : IRequestHandler<GetBatchQuery, BatchResponse?>
{
    public async Task<BatchResponse?> Handle(GetBatchQuery query, CancellationToken cancellationToken)
    {
        var batch = await db.Batches.AsNoTracking()
            .Include(b => b.Items)
            .SingleOrDefaultAsync(b => b.Id == query.Id, cancellationToken);
        if (batch is null)
            return null;

        var items = batch.Items
            .OrderBy(i => i.Position)
            .Select(i => new BatchItemResponse(
                i.Position,
                i.ExternalId,
                i.Result,
                FieldErrors.GroupByField(i.Errors.Select(e => new FieldError(e.Field, e.Code, e.Message)))))
            .ToList();
        return new BatchResponse(batch.Id, batch.Type, batch.Status, batch.ReceivedAt, batch.CompletedAt, items);
    }
}
