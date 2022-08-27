using Llama.Airforce.Database.Models.Curve;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Llama.Airforce.Database.Contexts;

public class CurvePoolRatiosContext
{
    private readonly Container Container;

    public CurvePoolRatiosContext(
        CosmosClient dbClient,
        string dbName,
        string containerName)
    {
        Container = dbClient.GetContainer(dbName, containerName);
    }

    public async Task<List<CurvePoolRatios>> GetAllAsync()
    {
        try
        {
            var ratios = new List<CurvePoolRatios>();

            using var iter = Container.GetItemLinqQueryable<CurvePoolRatios>().ToFeedIterator();
            while (iter.HasMoreResults)
                ratios.AddRange(await iter.ReadNextAsync());

            return ratios;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new List<CurvePoolRatios>();
        }
    }

    public async Task UpsertAsync(CurvePoolRatios ratios)
    {
        await Container.UpsertItemAsync(ratios, new PartitionKey(ratios.Name));
    }

    public static async Task<CurvePoolRatiosContext> Create(
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

        var containerName = "CurvePoolRatios";
        var database = await dbClient.CreateDatabaseIfNotExistsAsync(dbName);
        await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

        return new CurvePoolRatiosContext(
            dbClient: dbClient,
            dbName: dbName,
            containerName: containerName);
    }
}