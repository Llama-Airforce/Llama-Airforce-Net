using Llama.Airforce.Database.Models.Curve;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Llama.Airforce.Database.Contexts;

public class CurvePoolContext
{
    private readonly Container Container;

    public CurvePoolContext(
        CosmosClient dbClient,
        string dbName,
        string containerName)
    {
        Container = dbClient.GetContainer(dbName, containerName);
    }

    public async Task<List<Pool>> GetAllAsync()
    {
        try
        {
            var pools = new List<Pool>();

            using var iter = Container.GetItemLinqQueryable<Pool>().ToFeedIterator();
            while (iter.HasMoreResults)
                pools.AddRange(await iter.ReadNextAsync());

            return pools;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new List<Pool>();
        }
    }

    public async Task UpsertAsync(Pool pool)
    {
        await Container.UpsertItemAsync(pool, new PartitionKey(pool.Name));
    }

    public static async Task<CurvePoolContext> Create(
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

        var containerName = "CurvePools";
        var database = await dbClient.CreateDatabaseIfNotExistsAsync(dbName);
        await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

        return new CurvePoolContext(
            dbClient: dbClient,
            dbName: dbName,
            containerName: containerName);
    }
}