using Microsoft.EntityFrameworkCore;
using PmfApi.Infrastructure.Persistence;

namespace PmfApi.Tests.Support;

public static class TestDb
{
    public static PmfDbContext Create(string? name = null)
    {
        var options = new DbContextOptionsBuilder<PmfDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .Options;

        return new PmfDbContext(options);
    }
}
