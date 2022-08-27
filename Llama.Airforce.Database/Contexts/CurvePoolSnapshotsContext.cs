using LanguageExt;
using Llama.Airforce.Database.Models.Curve;
using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;

namespace Llama.Airforce.Database.Contexts;

public class CurvePoolSnapshotsContext
{
    private readonly Container Container;

    public CurvePoolSnapshotsContext(
        CosmosClient dbClient,
        string dbName,
        string containerName)
    {
        Container = dbClient.GetContainer(dbName, containerName);
    }

    public async Task<Option<CurvePoolSnapshots>> GetAsync(string pool)
    {
        try
        {
            var resp = await Container.ReadItemAsync<CurvePoolSnapshots>(pool, new PartitionKey(pool));
            return resp.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Option<CurvePoolSnapshots>.None;
        }
    }

    public async Task UpsertAsync(CurvePoolSnapshots snapshots)
    {
        await Container.UpsertItemAsync(snapshots, new PartitionKey(snapshots.Name));
    }

    public static async Task<CurvePoolSnapshotsContext> Create(
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

        var containerName = "CurvePoolSnapshots";
        var database = await dbClient.CreateDatabaseIfNotExistsAsync(dbName);
        await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

        return new CurvePoolSnapshotsContext(
            dbClient: dbClient,
            dbName: dbName,
            containerName: containerName);
    }
}