using Microsoft.EntityFrameworkCore;
using YMS.Domain.Batches;
using YMS.Domain.Categories;
using YMS.Domain.Products;
using YMS.Domain.Suppliers;

namespace YMS.Application.Common;

/// <summary>The single unit of work. Owner data sets are already filtered to the current Owner.</summary>
public interface IYmsDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<Batch> Batches { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs <paramref name="work"/> in one database transaction.</summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken);
}
