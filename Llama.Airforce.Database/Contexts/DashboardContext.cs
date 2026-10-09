using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Llama.Airforce.Database.Contexts;

public class DashboardContext
{
    private readonly Container Container;

    public DashboardContext(
        CosmosClient dbClient,
        string dbName,
        string containerName)
    {
        Container = dbClient.GetContainer(dbName, containerName);
    }

    public async Task UpsertAsync<T>(T info) where T : Dashboard
    {
        await Container.UpsertItemAsync(info, new PartitionKey(info.Id));
    }

    public static async Task<DashboardContext> Create(
        string endpointUri,
        string primaryKey,
        string dbName)
    {
        var dbClient = new CosmosClient(
            accountEndpoint: endpointUri,
            authKeyOrResourceToken: primaryKey,
            new CosmosClientOptions()
            {
                ApplicationName = "LlamaAirforce",
            });

        var containerName = "Dashboards";
        var database = await dbClient.CreateDatabaseIfNotExistsAsync(dbName);
        await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

        return new DashboardContext(
            dbClient: dbClient,
            dbName: dbName,
            containerName: containerName);
    }
}
