using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Propostas.Infrastructure.Persistence;

internal sealed class PropostasDbContextFactory : IDesignTimeDbContextFactory<PropostasDbContext>
{
    public PropostasDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PropostasDbContext>()
            .UseSqlServer("Server=localhost;Database=propostas_db;Trusted_Connection=False;")
            .Options;

        return new PropostasDbContext(options);
    }
}
