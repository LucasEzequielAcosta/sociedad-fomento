using SociedadFomento.Application.Abstractions;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Authentication;

/// <summary>Creates the first administrator idempotently from external configuration.</summary>
public sealed class AdminBootstrapService
{
    private readonly IAdminAuthenticationContext context;
    private readonly IAdminPasswordService passwordService;
    private readonly IClock clock;

    /// <summary>Creates the administrator bootstrap service.</summary>
    public AdminBootstrapService(IAdminAuthenticationContext context, IAdminPasswordService passwordService, IClock clock)
    {
        this.context = context;
        this.passwordService = passwordService;
        this.clock = clock;
    }

    /// <summary>Creates an administrator only when complete bootstrap credentials are supplied and no matching email exists.</summary>
    public Task BootstrapAsync(string? email, string? password, CancellationToken cancellationToken = default)
    {
        if (email is null && password is null)
        {
            return Task.CompletedTask;
        }
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Admin bootstrap requires both email and password configuration values.");
        }

        return BootstrapConfiguredAsync(email, password, cancellationToken);
    }

    private async Task BootstrapConfiguredAsync(string email, string password, CancellationToken cancellationToken)
    {
        string normalizedEmail = EmailNormalizer.Normalize(email);
        if (await context.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken) is not null)
        {
            return;
        }

        AdminUser user = AdminUser.Create(email, clock.UtcNow, admin => passwordService.HashPassword(admin, password));
        await context.TryAddAsync(user, cancellationToken);
    }
}
