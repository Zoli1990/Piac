using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RekeszAppBackend.Data;

// A dotnet ef tooling ezt a factory-t részesíti előnyben a Program.cs-ben
// beállított DI-konfigurációval szemben, ezért itt is a valódi appsettings.json-t
// kell olvasni - soha nem hardcode-olt connection stringet.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("A 'DefaultConnection' nincs beállítva az appsettings.json-ban.");

        var options = new DbContextOptionsBuilder<AppDbContext>();
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)));
        return new AppDbContext(options.Options);
    }
}
