using Llama.Airforce.Database.Contexts;

namespace Llama.Airforce.API.Builder;

public static class Contexts
{
    public static void AddContexts(this IServiceCollection services)
    {
        services.AddSingleton(services =>
        {
            var config = services.GetService<IConfiguration>();
            var endpointUri = config.GetValue<string>("DB_ENDPOINT");
            var primaryKey = config.GetValue<string>("DB_PRIMARY_KEY");
            var dbName = config.GetValue<string>("DB_NAME");

            return DashboardContext
                .Create(
                    endpointUri: endpointUri,
                    primaryKey: primaryKey,
                    dbName: dbName)
                .GetAwaiter()
                .GetResult();
        });

        services.AddSingleton(services =>
        {
            var config = services.GetService<IConfiguration>();
            var endpointUri = config.GetValue<string>("DB_ENDPOINT");
            var primaryKey = config.GetValue<string>("DB_PRIMARY_KEY");
            var dbName = config.GetValue<string>("DB_NAME");

            return PoolContext
                .Create(
                    endpointUri: endpointUri,
                    primaryKey: primaryKey,
                    dbName: dbName)
                .GetAwaiter()
                .GetResult();
        });

        services.AddSingleton(services =>
        {
            var config = services.GetService<IConfiguration>();
            var endpointUri = config.GetValue<string>("DB_ENDPOINT");
            var primaryKey = config.GetValue<string>("DB_PRIMARY_KEY");
            var dbName = config.GetValue<string>("DB_NAME");

            return PoolSnapshotsContext
                .Create(
                    endpointUri: endpointUri,
                    primaryKey: primaryKey,
                    dbName: dbName)
                .GetAwaiter()
                .GetResult();
        });

        services.AddSingleton(services =>
        {
            var config = services.GetService<IConfiguration>();
            var endpointUri = config.GetValue<string>("DB_ENDPOINT");
            var primaryKey = config.GetValue<string>("DB_PRIMARY_KEY");
            var dbName = config.GetValue<string>("DB_NAME");

            return CurvePoolContext
                .Create(
                    endpointUri: endpointUri,
                    primaryKey: primaryKey,
                    dbName: dbName)
                .GetAwaiter()
                .GetResult();
        });

        services.AddSingleton(services =>
        {
            var config = services.GetService<IConfiguration>();
            var endpointUri = config.GetValue<string>("DB_ENDPOINT");
            var primaryKey = config.GetValue<string>("DB_PRIMARY_KEY");
            var dbName = config.GetValue<string>("DB_NAME");

            return CurvePoolSnapshotsContext
                .Create(
                    endpointUri: endpointUri,
                    primaryKey: primaryKey,
                    dbName: dbName)
                .GetAwaiter()
                .GetResult();
        });

        services.AddSingleton(services =>
        {
            var config = services.GetService<IConfiguration>();
            var endpointUri = config.GetValue<string>("DB_ENDPOINT");
            var primaryKey = config.GetValue<string>("DB_PRIMARY_KEY");
            var dbName = config.GetValue<string>("DB_NAME");

            return CurvePoolRatiosContext
                .Create(
                    endpointUri: endpointUri,
                    primaryKey: primaryKey,
                    dbName: dbName)
                .GetAwaiter()
                .GetResult();
        });

        services.AddSingleton(services =>
        {
            var config = services.GetService<IConfiguration>();
            var endpointUri = config.GetValue<string>("DB_ENDPOINT");
            var primaryKey = config.GetValue<string>("DB_PRIMARY_KEY");
            var dbName = config.GetValue<string>("DB_NAME");

            return BribesContext
                .Create(
                    endpointUri: endpointUri,
                    primaryKey: primaryKey,
                    dbName: dbName)
                .GetAwaiter()
                .GetResult();
        });

        services.AddSingleton(services =>
        {
            var config = services.GetService<IConfiguration>();
            var endpointUri = config.GetValue<string>("DB_ENDPOINT");
            var primaryKey = config.GetValue<string>("DB_PRIMARY_KEY");
            var dbName = config.GetValue<string>("DB_NAME");

            return AirdropContext
                .Create(
                    endpointUri: endpointUri,
                    primaryKey: primaryKey,
                    dbName: dbName)
                .GetAwaiter()
                .GetResult();
        });
    }
}