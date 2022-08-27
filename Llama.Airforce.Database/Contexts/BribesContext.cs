using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
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

    public async Task<List<int>> Rounds(string platform, string protocol)
    {
        try
        {
            var rounds = new List<int>();

            using var iter = Container
                .GetItemLinqQueryable<Epoch>()
                .Where(epoch => epoch.Platform == platform && epoch.Protocol == protocol)
                .Select(epoch => epoch.Round)
                .ToFeedIterator();

            while (iter.HasMoreResults)
                rounds.AddRange(await iter.ReadNextAsync());

            return rounds.OrderBy(x => x).ToList();
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new List<int>();
        }
    }

    public async Task<Option<Epoch>> GetAsync(EpochId epochId)
    {
        try
        {
            var resp = await Container.ReadItemAsync<Epoch>(epochId, new PartitionKey(epochId));
            return resp.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Option<Epoch>.None;
        }
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

    public async Task UpsertAsync(Epoch epoch)
    {
        epoch.Id = (EpochId)epoch;
        await Container.UpsertItemAsync(epoch, new PartitionKey((EpochId)epoch));
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