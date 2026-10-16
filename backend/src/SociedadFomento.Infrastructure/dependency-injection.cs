using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Application.Abstractions;
using SociedadFomento.Application.Accounting;
using SociedadFomento.Application.Authentication;
using SociedadFomento.Application.Dashboard;
using SociedadFomento.Application.FeeGeneration;
using SociedadFomento.Application.FeeRates;
using SociedadFomento.Application.MemberStatements;
using SociedadFomento.Application.Members;
using SociedadFomento.Application.MemberPortal;
using SociedadFomento.Application.Payments;
using SociedadFomento.Infrastructure.Payments;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Infrastructure.Accounting;
using SociedadFomento.Infrastructure.Authentication;
using SociedadFomento.Infrastructure.Dashboard;
using SociedadFomento.Infrastructure.FeeGeneration;
using SociedadFomento.Infrastructure.FeeRates;
using SociedadFomento.Infrastructure.MemberStatements;
using SociedadFomento.Infrastructure.Members;
using SociedadFomento.Infrastructure.MemberPortal;
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
        services.AddScoped<IFeeRateContext, FeeRateContext>();
        services.AddScoped<FeeRateService>();
        services.AddScoped<IFeeGenerationContext, FeeGenerationContext>();
        services.AddScoped<FeeGenerationService>();
        services.AddScoped<IPaymentContext, PaymentContext>();
        services.AddScoped<PaymentService>();
        services.AddScoped<IMemberStatementContext, MemberStatementContext>();
        services.AddScoped<MemberStatementService>();
        services.AddScoped<IMemberContext, MemberContext>();
        services.AddScoped<MemberService>();
        services.AddScoped<IPaymentCancellationContext, PaymentCancellationContext>();
        services.AddScoped<PaymentCancellationService>();
        services.AddScoped<IAccountingEntryContext, AccountingEntryContext>();
        services.AddScoped<AccountingEntryService>();
        services.AddScoped<IMemberPortalContext, MemberPortalContext>();
        services.AddScoped<MemberPortalService>();
        services.AddScoped<IDashboardContext, DashboardContext>();
        services.AddScoped<DashboardService>();
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}
