using YMS.Domain.Categories;
using YMS.Domain.Common;

namespace YMS.Domain.Products;

public sealed class Product : Entity
{
    /// <summary>The only unit of measure for now; recorded and returned, but not part of the input contract.</summary>
    public const string DefaultUnitOfMeasure = "UN";

    public const int MaxUnitOfMeasureLength = 10;

    private readonly List<Ean> _eans = [];

    public Guid OwnerId { get; private set; }
    public string ExternalId { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public string UnitOfMeasure { get; private set; } = DefaultUnitOfMeasure;
    public IReadOnlyList<Ean> Eans => _eans;

    private Product() { }

    public static Product Create(Guid ownerId, string externalId, string description, Category category,
        IReadOnlyList<Ean> eans, DateTimeOffset now)
    {
        var product = new Product { OwnerId = ownerId, ExternalId = externalId };
        product.Apply(description, category, eans);
        product.MarkCreated(now);
        return product;
    }

    /// <summary>Updates the product from a resend; the EAN list replaces the previous one.</summary>
    public void Update(string description, Category category, IReadOnlyList<Ean> eans, DateTimeOffset now)
    {
        Apply(description, category, eans);
        MarkUpdated(now);
    }

    private void Apply(string description, Category category, IReadOnlyList<Ean> eans)
    {
        // Check every invariant before changing anything, so a rejection leaves the Product untouched.
        if (category.OwnerId != OwnerId)
            throw new DomainException("categoryExternalId", "category.not_found", "A Categoria informada não existe.");
        var normalizedEans = NormalizeEans(eans).ToList();

        Description = description;
        Category = category;
        CategoryId = category.Id;
        _eans.Clear();
        _eans.AddRange(normalizedEans);
    }

    /// <summary>At least one EAN and exactly one primary; a lone EAN is the primary one.</summary>
    private static IEnumerable<Ean> NormalizeEans(IReadOnlyList<Ean> eans)
    {
        if (eans.Count == 0)
            throw new DomainException("eans", "product.ean_required", "O Produto precisa de pelo menos um EAN.");
        if (eans.Count == 1)
            return [new Ean(eans[0].Value, isPrimary: true)];

        var primaries = eans.Count(e => e.IsPrimary);
        if (primaries == 0)
            throw new DomainException("eans", "product.primary_ean_required", "O Produto precisa de um EAN principal.");
        if (primaries > 1)
            throw new DomainException("eans", "product.single_primary_ean", "O Produto deve ter apenas um EAN principal.");
        return eans;
    }
}

/// <summary>A barcode of the Product, kept as free text exactly as the ERP sends it.</summary>
public sealed class Ean
{
    public string Value { get; private set; }
    public bool IsPrimary { get; private set; }

    public Ean(string value, bool isPrimary)
    {
        Value = value;
        IsPrimary = isPrimary;
    }
}
