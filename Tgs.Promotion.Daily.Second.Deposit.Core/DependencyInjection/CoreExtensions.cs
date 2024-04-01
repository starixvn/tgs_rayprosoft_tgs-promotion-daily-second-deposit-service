using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Tgs.Promotion.Daily.Second.Deposit.Core.DependencyInjection;

public static class CoreExtensions
{
    public static IServiceCollection AddPromotionDailySecondDepositCore(this IServiceCollection services)
    {
        services
            .RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient);

        return services;
    }
}