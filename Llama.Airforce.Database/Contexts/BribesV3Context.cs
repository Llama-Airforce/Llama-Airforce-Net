using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Llama.Airforce.Database.Models.Bribes;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Llama.Airforce.Database.Contexts;

public class BribesV3Context
{
    private readonly Container Container;

    public BribesV3Context(
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
                .GetItemLinqQueryable<EpochV3>()
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

    public async Task<Option<EpochV3>> GetAsync(EpochId epochId)
    {
        try
        {
            var resp = await Container.ReadItemAsync<EpochV3>(epochId, new PartitionKey(epochId));
            return resp.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Option<EpochV3>.None;
        }
    }

    public async Task<List<EpochV3>> GetAllAsync(string platform, string protocol)
    {
        try
        {
            var epochs = new List<EpochV3>();

            using var iter = Container.GetItemLinqQueryable<EpochV3>()
                .Where(epoch => epoch.Platform == platform && epoch.Protocol == protocol)
                .ToFeedIterator();

            while (iter.HasMoreResults)
                epochs.AddRange(await iter.ReadNextAsync());

            return epochs;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new List<EpochV3>();
        }
    }

    public async Task UpsertAsync(EpochV3 epoch)
    {
        epoch.Id = (EpochId)epoch;
        await Container.UpsertItemAsync(epoch, new PartitionKey((EpochId)epoch));
    }

    public static async Task<BribesV3Context> Create(
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

        var containerName = "BribesV3";
        var database = await dbClient.CreateDatabaseIfNotExistsAsync(dbName);
        await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

        return new BribesV3Context(
            dbClient: dbClient,
            dbName: dbName,
            containerName: containerName);
    }
}