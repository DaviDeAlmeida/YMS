using YMS.Domain.Common;

namespace YMS.Domain.Suppliers;

/// <summary>A company registered by one Owner. The same CNPJ under another Owner is another Supplier.</summary>
public sealed class Supplier : Entity
{
    public Guid OwnerId { get; private set; }
    public string ExternalId { get; private set; } = null!;
    public string Name { get; private set; } = null!;

    /// <summary>Digits only. May repeat inside the Owner: only <see cref="ExternalId"/> is unique.</summary>
    public string Cnpj { get; private set; } = null!;

    public Address Address { get; private set; } = null!;

    private Supplier() { }

    public static Supplier Create(Guid ownerId, string externalId, string name, string cnpj, Address address, DateTimeOffset now)
    {
        var supplier = new Supplier { OwnerId = ownerId, ExternalId = externalId };
        supplier.Apply(name, cnpj, address);
        supplier.MarkCreated(now);
        return supplier;
    }

    public void Update(string name, string cnpj, Address address, DateTimeOffset now)
    {
        Apply(name, cnpj, address);
        MarkUpdated(now);
    }

    private void Apply(string name, string cnpj, Address address)
    {
        if (!Suppliers.Cnpj.IsValid(cnpj))
            throw new DomainException("cnpj", "supplier.cnpj_invalid", "O CNPJ informado é inválido.");

        Name = name;
        Cnpj = Suppliers.Cnpj.Normalize(cnpj);
        Address = address;
    }
}

public sealed record Address(
    string Country,
    string State,
    string City,
    string Street,
    string? Number,
    string? Complement,
    string? District,
    string ZipCode)
{
    public const string Brazil = "BR";

    /// <summary>The Brazilian states (UF) accepted when <see cref="Country"/> is <c>BR</c>.</summary>
    public static readonly IReadOnlySet<string> BrazilianStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA",
        "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO",
    };

    public static bool IsBrazil(string? country) => string.Equals(country?.Trim(), Brazil, StringComparison.OrdinalIgnoreCase);

    /// <summary>A Brazilian CEP without its mask: <c>01310-100</c> → <c>01310100</c>.</summary>
    public static string NormalizeBrazilianZipCode(string zipCode) => new(zipCode.Where(c => c is not ('-' or '.' or ' ')).ToArray());
}
