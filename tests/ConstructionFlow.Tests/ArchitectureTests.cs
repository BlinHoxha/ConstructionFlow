using ConstructionFlow.Application;
using Xunit;

namespace ConstructionFlow.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Application_workflows_do_not_depend_on_http_or_entity_framework()
    {
        var references = typeof(ProjectWorkflow).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
        Assert.DoesNotContain(references, x => x is not null &&
            (x.StartsWith("Microsoft.EntityFrameworkCore") || x.StartsWith("Microsoft.AspNetCore") ||
             x is "ConstructionFlow.Infrastructure" or "ConstructionFlow.Api"));
    }

    [Fact]
    public void ConstructionFlow_does_not_ship_unused_mapping_or_sql_server_packages()
    {
        var dependencies = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ConstructionFlow.Tests.deps.json"));
        Assert.DoesNotContain("AutoMapper/", dependencies);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore.SqlServer/", dependencies);
        Assert.DoesNotContain("Framework.Infrastructure/", dependencies);
    }
}
