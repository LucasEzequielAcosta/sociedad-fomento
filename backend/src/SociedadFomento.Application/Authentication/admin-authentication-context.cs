using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Authentication;

/// <summary>Defines specialized administrator persistence operations.</summary>
public interface IAdminAuthenticationContext
{
    Task<AdminUser?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<bool> TryAddAsync(AdminUser adminUser, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
