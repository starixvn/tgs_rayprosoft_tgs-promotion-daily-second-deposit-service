using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Tgs.Promotion.Daily.Second.Deposit.Core;
using Tgs.Promotion.Daily.Second.Deposit.Repository.Entities;

namespace Tgs.Promotion.Daily.Second.Deposit.Repository.DependencyInjection;

public static class TgsRepositoryExtensions
{
    public static IServiceCollection AddDailySecondDepositRepositories(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> dbConfigure,
        Func<IServiceProvider, IDatabase> resolveRedisDatabase)
        => services
            .AddDbContext<TgsContext>(dbConfigure)
            .AddScoped<ITgsRepository, TgsRepository>();
}