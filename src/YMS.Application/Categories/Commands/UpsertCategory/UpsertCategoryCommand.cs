using MediatR;
using YMS.Application.Common;

namespace YMS.Application.Categories.Commands.UpsertCategory;

public sealed record UpsertCategoryCommand(string? ExternalId, string? Description)
    : IRequest<UpsertResult<CategoryResponse>>;
