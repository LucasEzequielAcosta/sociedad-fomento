using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SociedadFomento.Application.Authentication;

namespace SociedadFomento.Infrastructure.Authentication;

internal sealed class AdminBootstrapHostedService : IHostedService
{
    private readonly IConfiguration configuration;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<AdminBootstrapHostedService> logger;

    public AdminBootstrapHostedService(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<AdminBootstrapHostedService> logger)
    {
        this.configuration = configuration;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        string? email = configuration["AdminBootstrap:Email"];
        string? password = configuration["AdminBootstrap:Password"];
        if (email is null && password is null)
        {
            return;
        }

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        AdminBootstrapService service = scope.ServiceProvider.GetRequiredService<AdminBootstrapService>();
        try
        {
            await service.BootstrapAsync(email, password, cancellationToken);
        }
        catch (SqlException exception) when (exception.Number == 208)
        {
            logger.LogError("Authentication schema is unavailable. Apply migrations before enabling administrator bootstrap.");
            throw new InvalidOperationException("Authentication schema is unavailable. Apply migrations before administrator bootstrap.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
