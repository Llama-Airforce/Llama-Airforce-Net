using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Llama.Airforce.Database.Models.Bribes;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Llama.Airforce.Database.Contexts;

public class BribesV2Context
{
    private readonly Container Container;

    public BribesV2Context(
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
                .GetItemLinqQueryable<EpochV2>()
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

    public async Task<Option<EpochV2>> GetAsync(EpochId epochId)
    {
        try
        {
            var resp = await Container.ReadItemAsync<EpochV2>(epochId, new PartitionKey(epochId));
            return resp.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Option<EpochV2>.None;
        }
    }

    public async Task<List<EpochV2>> GetAllAsync(string platform, string protocol)
    {
        try
        {
            var epochs = new List<EpochV2>();

            using var iter = Container.GetItemLinqQueryable<EpochV2>()
                .Where(epoch => epoch.Platform == platform && epoch.Protocol == protocol)
                .ToFeedIterator();

            while (iter.HasMoreResults)
                epochs.AddRange(await iter.ReadNextAsync());

            return epochs;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new List<EpochV2>();
        }
    }

    public async Task UpsertAsync(EpochV2 epoch)
    {
        epoch.Id = (EpochId)epoch;
        await Container.UpsertItemAsync(epoch, new PartitionKey((EpochId)epoch));
    }

    public static async Task<BribesV2Context> Create(
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

        var containerName = "BribesV2";
        var database = await dbClient.CreateDatabaseIfNotExistsAsync(dbName);
        await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

        return new BribesV2Context(
            dbClient: dbClient,
            dbName: dbName,
            containerName: containerName);
    }
}