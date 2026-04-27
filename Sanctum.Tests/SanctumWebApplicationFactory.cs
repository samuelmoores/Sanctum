using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sanctum.Data;

namespace Sanctum.Tests;

// Boots the real Sanctum app pipeline (routing, auth, DI, middleware) but
// swaps the Postgres AppDbContext for an in-memory one shared across
// all requests in this factory instance.
public class SanctumWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DbName { get; } = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            var inMemoryEfServices = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<AppDbContext>(opts =>
                opts.UseInMemoryDatabase(DbName)
                    .UseInternalServiceProvider(inMemoryEfServices));
        });
    }
}
