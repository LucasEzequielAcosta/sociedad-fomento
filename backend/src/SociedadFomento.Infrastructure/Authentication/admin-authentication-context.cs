using System.Runtime.ExceptionServices;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SociedadFomento.Application.Authentication;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Authentication;

internal sealed class AdminAuthenticationContext : IAdminAuthenticationContext
{
    private readonly SociedadFomentoDbContext dbContext;
    private readonly DbContextOptions<SociedadFomentoDbContext> options;

    public AdminAuthenticationContext(
        SociedadFomentoDbContext dbContext,
        DbContextOptions<SociedadFomentoDbContext> options)
    {
        this.dbContext = dbContext;
        this.options = options;
    }

    public Task<AdminUser?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        dbContext.AdminUsers.SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task<bool> TryAddAsync(AdminUser adminUser, CancellationToken cancellationToken)
    {
        Exception? raceException = null;
        await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken))
        {
            dbContext.AdminUsers.Add(adminUser);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch (Exception exception) when (IsKnownRace(exception))
            {
                raceException = exception;
                await transaction.RollbackAsync(cancellationToken);
            }
        }

        dbContext.ChangeTracker.Clear();
        await using SociedadFomentoDbContext verificationContext = new(options);
        bool exists = await verificationContext.AdminUsers.AsNoTracking().AnyAsync(
            user => user.NormalizedEmail == adminUser.NormalizedEmail, cancellationToken);
        if (exists)
        {
            return false;
        }

        ExceptionDispatchInfo.Capture(raceException!).Throw();
        return false;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            T result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool IsKnownRace(Exception exception)
    {
        SqlException? sqlException = exception as SqlException ?? exception.InnerException as SqlException;
        return sqlException?.Number == 1205 ||
            sqlException?.Number is 2601 or 2627 &&
            sqlException.Message.Contains("IX_AdminUsers_NormalizedEmail", StringComparison.OrdinalIgnoreCase);
    }
}
