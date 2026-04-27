using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Sanctum.Data;

namespace Sanctum.SystemTests;

// System tests need a HTTP port the browser can connect to —
// the in-process TestServer used by integration tests isn't reachable
// from a separate process (or even Playwright in the same process).
// This factory boots Kestrel on a random localhost port and exposes
// the URL via ServerAddress.
public class PlaywrightWebApplicationFactory : WebApplicationFactory<Program>
{
    private IHost? _kestrelHost;
    private readonly string _dbName = Guid.NewGuid().ToString();

    private readonly IServiceProvider _inMemoryEfServices = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

    public string ServerAddress
    {
        get
        {
            EnsureServer();
            return _kestrelHost!.Services
                .GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!
                .Addresses.First();
        }
    }

    private void EnsureServer()
    {
        // Triggers CreateHost(), which starts our Kestrel host as a side effect.
        if (_kestrelHost is null)
        {
            using var _ = CreateDefaultClient();
        }
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = builder.Build();
        
        builder.ConfigureWebHost(b => b.UseKestrel().UseUrls("http://127.0.0.1:0"));
        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        testHost.Start();
        return testHost;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName)
                    .UseInternalServiceProvider(_inMemoryEfServices));
        });
    }

    protected override void Dispose(bool disposing)
    {
        _kestrelHost?.Dispose();
        base.Dispose(disposing);
    }
}
