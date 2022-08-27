using LanguageExt;
using Llama.Airforce.Database.Models.Convex;
using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;

namespace Llama.Airforce.Database.Contexts;

public class PoolSnapshotsContext
{
    private readonly Container Container;

    public PoolSnapshotsContext(
        CosmosClient dbClient,
        string dbName,
        string containerName)
    {
        Container = dbClient.GetContainer(dbName, containerName);
    }

    public async Task<Option<PoolSnapshots>> GetAsync(string pool)
    {
        try
        {
            var resp = await Container.ReadItemAsync<PoolSnapshots>(pool, new PartitionKey(pool));
            return resp.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Option<PoolSnapshots>.None;
        }
    }

    public async Task UpsertAsync(PoolSnapshots snapshots)
    {
        await Container.UpsertItemAsync(snapshots, new PartitionKey(snapshots.Name));
    }

    public static async Task<PoolSnapshotsContext> Create(
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

        var containerName = "PoolSnapshots";
        var database = await dbClient.CreateDatabaseIfNotExistsAsync(dbName);
        await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

        return new PoolSnapshotsContext(
            dbClient: dbClient,
            dbName: dbName,
            containerName: containerName);
    }
}