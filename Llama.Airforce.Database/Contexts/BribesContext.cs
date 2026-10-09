using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Llama.Airforce.Database.Models.Bribes;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Llama.Airforce.Database.Contexts;

public class BribesContext
{
    private readonly Container Container;

    public BribesContext(
        CosmosClient dbClient,
        string dbName,
        string containerName)
    {
        Container = dbClient.GetContainer(dbName, containerName);
    }

    public async Task<List<Epoch>> GetAllAsync(string platform, string protocol)
    {
        try
        {
            var epochs = new List<Epoch>();

            using var iter = Container.GetItemLinqQueryable<Epoch>()
                .Where(epoch => epoch.Platform == platform && epoch.Protocol == protocol)
                .ToFeedIterator();

            while (iter.HasMoreResults)
                epochs.AddRange(await iter.ReadNextAsync());

            return epochs;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new List<Epoch>();
        }
    }

    public static async Task<BribesContext> Create(
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

        var containerName = "Bribes";
        var database = await dbClient.CreateDatabaseIfNotExistsAsync(dbName);
        await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

        return new BribesContext(
            dbClient: dbClient,
            dbName: dbName,
            containerName: containerName);
    }
}
