using Llama.Airforce.Database.Contexts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Llama.Airforce.Jobs.Extensions;

public static class ServiceCollectionExt
{
    public static IServiceCollection AddContexts(this IServiceCollection services, IConfiguration configuration) => services.AddTransient((_) =>
        {
            var endpointUri = configuration.GetValue<string>("DB_ENDPOINT");
            var primaryKey = configuration.GetValue<string>("DB_PRIMARY_KEY");
            var dbName = configuration.GetValue<string>("DB_NAME");

            return DashboardContext
                .Create(
                    endpointUri: endpointUri,
                    primaryKey: primaryKey,
                    dbName: dbName)
                .GetAwaiter()
                .GetResult();
        })
        .AddTransient((_) =>
        {
            var endpointUri = configuration.GetValue<string>("DB_ENDPOINT");
            var primaryKey = configuration.GetValue<string>("DB_PRIMARY_KEY");
            var dbName = configuration.GetValue<string>("DB_NAME");

            return PoolContext
                .Create(
                    endpointUri: endpointUri,
                    primaryKey: primaryKey,
                    dbName: dbName)
                .GetAwaiter()
                .GetResult();
        })
        .AddTransient((_) =>
        {
            var endpointUri = configuration.GetValue<string>("DB_ENDPOINT");
            var primaryKey = configuration.GetValue<string>("DB_PRIMARY_KEY");
            var dbName = configuration.GetValue<string>("DB_NAME");

            return PoolSnapshotsContext
                .Create(
                    endpointUri: endpointUri,
                    primaryKey: primaryKey,
                    dbName: dbName)
                .GetAwaiter()
                .GetResult();
        })
        .AddTransient((_) =>
        {
            var endpointUri = configuration.GetValue<string>("DB_ENDPOINT");
            var primaryKey = configuration.GetValue<string>("DB_PRIMARY_KEY");
            var dbName = configuration.GetValue<string>("DB_NAME");

            return CurvePoolContext
                .Create(
                    endpointUri: endpointUri,
                    primaryKey: primaryKey,
                    dbName: dbName)
                .GetAwaiter()
                .GetResult();
        })
        .AddTransient((_) =>
        {
            var endpointUri = configuration.GetValue<string>("DB_ENDPOINT");
            var primaryKey = configuration.GetValue<string>("DB_PRIMARY_KEY");
            var dbName = configuration.GetValue<string>("DB_NAME");

            return CurvePoolSnapshotsContext
                .Create(
                    endpointUri: endpointUri,
                    primaryKey: primaryKey,
                    dbName: dbName)
                .GetAwaiter()
                .GetResult();
        })
        .AddTransient((_) =>
        {
            var endpointUri = configuration.GetValue<string>("DB_ENDPOINT");
            var primaryKey = configuration.GetValue<string>("DB_PRIMARY_KEY");
            var dbName = configuration.GetValue<string>("DB_NAME");

            return BribesContext
                .Create(
                    endpointUri: endpointUri,
                    primaryKey: primaryKey,
                    dbName: dbName)
                .GetAwaiter()
                .GetResult();
        })
       .AddTransient((_) =>
        {
            var endpointUri = configuration.GetValue<string>("DB_ENDPOINT");
            var primaryKey = configuration.GetValue<string>("DB_PRIMARY_KEY");
            var dbName = configuration.GetValue<string>("DB_NAME");

            return BribesV2Context
               .Create(
                    endpointUri: endpointUri,
                    primaryKey: primaryKey,
                    dbName: dbName)
               .GetAwaiter()
               .GetResult();
        })
        .AddTransient((_) =>
        {
            var endpointUri = configuration.GetValue<string>("DB_ENDPOINT");
            var primaryKey = configuration.GetValue<string>("DB_PRIMARY_KEY");
            var dbName = configuration.GetValue<string>("DB_NAME");

            return BribesV3Context
               .Create(
                    endpointUri: endpointUri,
                    primaryKey: primaryKey,
                    dbName: dbName)
               .GetAwaiter()
               .GetResult();
        });
}