using SociedadFomento.Application.Authentication;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Tests;

internal sealed class FakeAdminAuthenticationContext : IAdminAuthenticationContext
{
    internal List<AdminUser> Users { get; } = [];
    internal int SaveCount { get; private set; }

    public Task<AdminUser?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(user => user.NormalizedEmail == normalizedEmail));

    public Task<bool> TryAddAsync(AdminUser adminUser, CancellationToken cancellationToken)
    {
        if (Users.Any(user => user.NormalizedEmail == adminUser.NormalizedEmail))
        {
            return Task.FromResult(false);
        }
        Users.Add(adminUser);
        return Task.FromResult(true);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken) =>
        operation(cancellationToken);
}
