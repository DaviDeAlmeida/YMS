using System.Reflection;
using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using YMS.Application.Common;
using YMS.Domain.Common;
using YMS.Infrastructure.Persistence;

namespace YMS.Architecture.Tests;

/// <summary>The layer and folder rules of docs/design/modelagem-dominio.md.</summary>
public sealed partial class LayerTests
{
    private static readonly Assembly Domain = typeof(Entity).Assembly;
    private static readonly Assembly Application = typeof(Limits).Assembly;
    private static readonly Assembly Infrastructure = typeof(YmsDbContext).Assembly;

    [Fact]
    public void Domain_depends_only_on_the_base_library()
    {
        var references = ReferenceNames(Domain).Where(name => !name.StartsWith("System", StringComparison.Ordinal));

        Assert.Empty(references);
    }

    [Fact]
    public void Application_depends_on_no_outer_layer()
    {
        var references = ReferenceNames(Application)
            .Where(name => name is "YMS.Infrastructure" or "YMS.Api" || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));

        Assert.Empty(references);
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_the_Api()
    {
        var references = ReferenceNames(Infrastructure)
            .Where(name => name == "YMS.Api" || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));

        Assert.Empty(references);
    }

    [Fact]
    public void Use_cases_live_in_area_Commands_or_Queries_folders()
    {
        var misplaced = ApplicationTypes()
            .Where(IsUseCasePart)
            .Where(t => !UseCaseNamespace().IsMatch(t.Namespace ?? ""))
            .Select(t => t.FullName);

        Assert.Empty(misplaced);
    }

    [Fact]
    public void Responses_live_at_the_area_root()
    {
        var misplaced = ApplicationTypes()
            .Where(t => t.Name.EndsWith("Response", StringComparison.Ordinal))
            .Where(t => !AreaNamespace().IsMatch(t.Namespace ?? ""))
            .Select(t => t.FullName);

        Assert.Empty(misplaced);
    }

    private static IEnumerable<string> ReferenceNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!).Where(name => name != "netstandard");

    private static IEnumerable<Type> ApplicationTypes() =>
        Application.GetTypes().Where(t => !t.IsNested && !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)));

    private static bool IsUseCasePart(Type type) =>
        type.GetInterfaces().Any(i =>
            i == typeof(IBaseRequest)
            || i == typeof(IValidator)
            || i.IsGenericType && i.GetGenericTypeDefinition() is var definition
                && (definition == typeof(IRequestHandler<,>) || definition == typeof(IRequestHandler<>)));

    [GeneratedRegex(@"^YMS\.Application\.[A-Z]\w+\.(Commands|Queries)\.[A-Z]\w+$")]
    private static partial Regex UseCaseNamespace();

    [GeneratedRegex(@"^YMS\.Application\.(?!Common$)[A-Z]\w+$")]
    private static partial Regex AreaNamespace();
}
