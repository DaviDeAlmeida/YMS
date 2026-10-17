using Microsoft.EntityFrameworkCore;
using YMS.Application.Common;
using YMS.Domain.Batches;
using YMS.Domain.Categories;
using YMS.Domain.Owners;
using YMS.Domain.Products;
using YMS.Domain.Suppliers;

namespace YMS.Infrastructure.Persistence;

public sealed class YmsDbContext(DbContextOptions<YmsDbContext> options, IOwnerContext ownerContext)
    : DbContext(options), IYmsDbContext
{
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Batch> Batches => Set<Batch>();

    public const string ConnectionStringName = "Yms";

    // Enums are stored by name; the longest name is well under this.
    private const int MaxEnumLength = 20;

    /// <summary>PostgreSQL with snake_case columns; shared by the application and the design-time factory used by migrations.</summary>
    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure())
            .UseSnakeCaseNamingConvention();

    // Read through a property so EF evaluates it per query, against this scope's Owner.
    private Guid CurrentOwnerId => ownerContext.OwnerId;

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        await Database.CreateExecutionStrategy().ExecuteAsync(async ct =>
        {
            await using var transaction = await Database.BeginTransactionAsync(ct);
            await work(ct);
            await transaction.CommitAsync(ct);
        }, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Owner>(owner =>
        {
            owner.ToTable("owners");
            owner.HasKey(o => o.Id);
            owner.Property(o => o.Id).ValueGeneratedNever();
            owner.Property(o => o.Name).HasMaxLength(Limits.Name).IsRequired();
        });

        modelBuilder.Entity<Category>(category =>
        {
            category.ToTable("categories");
            category.HasKey(c => c.Id);
            category.Property(c => c.Id).ValueGeneratedNever();
            category.Property(c => c.ExternalId).HasMaxLength(Limits.ExternalId).IsRequired();
            category.Property(c => c.Description).HasMaxLength(Limits.Description).IsRequired();
            category.HasOne<Owner>().WithMany().HasForeignKey(c => c.OwnerId);
            category.HasIndex(c => new { c.OwnerId, c.ExternalId }).IsUnique();
            category.HasQueryFilter(c => c.OwnerId == CurrentOwnerId);
        });

        modelBuilder.Entity<Product>(product =>
        {
            product.ToTable("products");
            product.HasKey(p => p.Id);
            product.Property(p => p.Id).ValueGeneratedNever();
            product.Property(p => p.ExternalId).HasMaxLength(Limits.ExternalId).IsRequired();
            product.Property(p => p.Description).HasMaxLength(Limits.Description).IsRequired();
            product.Property(p => p.UnitOfMeasure).HasMaxLength(Product.MaxUnitOfMeasureLength).IsRequired();
            product.HasOne<Owner>().WithMany().HasForeignKey(p => p.OwnerId);
            product.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId);
            product.HasIndex(p => new { p.OwnerId, p.ExternalId }).IsUnique();
            product.OwnsMany(p => p.Eans, ean =>
            {
                ean.ToTable("product_eans");
                // The Owner travels in the key to the Product, so an EAN can only belong to a Product of its own Owner.
                ean.WithOwner().HasForeignKey("OwnerId", "ProductId").HasPrincipalKey(p => new { p.OwnerId, p.Id });
                ean.Property<Guid>("Id").HasValueGenerator<GuidV7ValueGenerator>();
                ean.HasKey("Id");
                ean.Property(e => e.Value).IsRequired();
            });
            product.Navigation(p => p.Eans).UsePropertyAccessMode(PropertyAccessMode.Field);
            product.HasQueryFilter(p => p.OwnerId == CurrentOwnerId);
        });

        modelBuilder.Entity<Supplier>(supplier =>
        {
            supplier.ToTable("suppliers");
            supplier.HasKey(s => s.Id);
            supplier.Property(s => s.Id).ValueGeneratedNever();
            supplier.Property(s => s.ExternalId).HasMaxLength(Limits.ExternalId).IsRequired();
            supplier.Property(s => s.Name).HasMaxLength(Limits.Name).IsRequired();
            supplier.Property(s => s.Cnpj).HasMaxLength(Cnpj.Length).IsRequired();
            supplier.HasOne<Owner>().WithMany().HasForeignKey(s => s.OwnerId);
            supplier.HasIndex(s => new { s.OwnerId, s.ExternalId }).IsUnique();
            supplier.HasIndex(s => new { s.OwnerId, s.Cnpj });
            supplier.OwnsOne(s => s.Address, address =>
            {
                address.Property(a => a.Country).HasMaxLength(Limits.Country).IsRequired();
                address.Property(a => a.State).HasMaxLength(Limits.AddressPart).IsRequired();
                address.Property(a => a.City).HasMaxLength(Limits.AddressPart).IsRequired();
                address.Property(a => a.Street).HasMaxLength(Limits.Street).IsRequired();
                address.Property(a => a.Number).HasMaxLength(Limits.AddressNumber);
                address.Property(a => a.Complement).HasMaxLength(Limits.AddressPart);
                address.Property(a => a.District).HasMaxLength(Limits.AddressPart);
                address.Property(a => a.ZipCode).HasMaxLength(Limits.ZipCode).IsRequired();
            });
            supplier.Navigation(s => s.Address).IsRequired();
            supplier.HasQueryFilter(s => s.OwnerId == CurrentOwnerId);
        });

        modelBuilder.Entity<Batch>(batch =>
        {
            batch.ToTable("batches");
            batch.HasKey(b => b.Id);
            batch.Property(b => b.Id).ValueGeneratedNever();
            batch.Property(b => b.Type).HasConversion<string>().HasMaxLength(MaxEnumLength);
            batch.Property(b => b.Status).HasConversion<string>().HasMaxLength(MaxEnumLength);
            batch.HasOne<Owner>().WithMany().HasForeignKey(b => b.OwnerId);
            batch.HasIndex(b => new { b.Status, b.ReceivedAt });
            batch.HasMany(b => b.Items).WithOne()
                .HasForeignKey(i => new { i.OwnerId, i.BatchId })
                .HasPrincipalKey(b => new { b.OwnerId, b.Id });
            batch.Navigation(b => b.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
            batch.HasQueryFilter(b => b.OwnerId == CurrentOwnerId);
        });

        modelBuilder.Entity<BatchItem>(item =>
        {
            item.ToTable("batch_items");
            item.HasKey(i => i.Id);
            item.Property(i => i.Id).ValueGeneratedNever();
            item.HasIndex(i => new { i.BatchId, i.Position }).IsUnique();
            item.Property(i => i.ExternalId).HasMaxLength(Limits.ExternalId);
            item.Property(i => i.Payload).IsRequired();
            item.Property(i => i.Result).HasConversion<string>().HasMaxLength(MaxEnumLength);
            item.Ignore(i => i.IsPending);
            item.OwnsMany(i => i.Errors, error =>
            {
                error.ToTable("batch_item_errors");
                error.WithOwner().HasForeignKey("OwnerId", "BatchItemId").HasPrincipalKey(i => new { i.OwnerId, i.Id });
                error.Property<Guid>("Id").HasValueGenerator<GuidV7ValueGenerator>();
                error.HasKey("Id");
            });
            item.Navigation(i => i.Errors).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}
