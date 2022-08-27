using System.Threading.Tasks;
using LanguageExt;
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

    public async Task<Option<T>> GetAsync<T>(string id) where T : Dashboard
    {
        try
        {
            var resp = await Container.ReadItemAsync<T>(id, new PartitionKey(id));
            return resp.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Option<T>.None;
        }
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