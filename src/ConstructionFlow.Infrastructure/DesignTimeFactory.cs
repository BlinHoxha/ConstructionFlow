using ConstructionFlow.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ConstructionFlow.Infrastructure;

public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<ConstructionDbContext>
{
    public ConstructionDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Construction")
            ?? "Host=localhost;Database=constructionflow;Username=constructionflow";
        return new(new DbContextOptionsBuilder<ConstructionDbContext>().UseNpgsql(connection).Options, new EmptyContext());
    }
    private sealed class EmptyContext : IRequestContext
    {
        public Guid TenantId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public string Subject => "";
    }
}
