using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Application.Abstractions;
using SociedadFomento.Application.Authentication;
using SociedadFomento.Application.Members;
using SociedadFomento.Application.Payments;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Infrastructure.Authentication;
using SociedadFomento.Infrastructure.Members;
using SociedadFomento.Infrastructure.Payments;
using SociedadFomento.Infrastructure.Persistence;
using SociedadFomento.Infrastructure.Time;

namespace SociedadFomento.Infrastructure;

/// <summary>Registers infrastructure services.</summary>
public static class DependencyInjection
{
    /// <summary>Registers SQL Server persistence using the supplied connection string.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SociedadFomentoDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IAdminAuthenticationContext, AdminAuthenticationContext>();
        services.AddScoped<IAdminPasswordService, AdminPasswordService>();
        services.AddScoped<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
        services.AddScoped<AdminAuthenticationService>();
        services.AddScoped<AdminBootstrapService>();
        services.AddHostedService<AdminBootstrapHostedService>();
        services.AddScoped<IMemberContext, MemberContext>();
        services.AddScoped<MemberService>();
        services.AddScoped<IPaymentCancellationContext, PaymentCancellationContext>();
        services.AddScoped<PaymentCancellationService>();
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}
