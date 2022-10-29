using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
using Llama.Airforce.Database.Models.Bribes;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using static LanguageExt.Prelude;

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

    public static List<string> MatchTokens = new() { "fxs", "crv", "cvxcrv" };

    public static bool HasMatchedToken(IEnumerable<string> tokens) => tokens
        .Any(token => MatchTokens.Contains(token));

    public static bool HasNativeToken(IEnumerable<string> tokens) => tokens
        .Any(token => !MatchTokens.Contains(token));

    /// <summary>
    /// Returns only bribes from an epoch that have Frax matches and that belong to an optional pool.
    /// </summary>
    public async Task<Option<(Epoch Epoch, List<Bribe> Bribes)>> GetFraxMatches(EpochId epochId, Option<List<string>> poolIds)
    {
        var epoch = await GetAsync(epochId);

        return epoch
            .Map(e =>
            {
                // Group bribes by the pools they're for.
                var pools = e.Bribes
                    // Optional pool id filter.
                    .Where(bribe => poolIds.Map(ids => ids.Contains(bribe.Pool)).IfNone(true))
                    .GroupBy(bribe => bribe.Pool)
                    .ToDictionary(gr => gr.Key, gr => gr.ToList());

                // Filter out pools that don't have Frax matches.
                var matchedPools = pools
                    .Where(x =>
                    {
                        var (_, bribes) = x;
                        var tokens = bribes.Map(bribe => bribe.Token.ToLowerInvariant()).ToList();

                        return HasMatchedToken(tokens) && HasNativeToken(tokens);
                    })
                    .Select(x => x.Key)
                    .ToList();

                return (
                    Epoch: e,
                    Bribes: matchedPools.SelectMany(pool => pools[pool]).ToList());
            })
            // Return 'None' if the epoch has no matched bribes at all.
            .Bind(e => !e.Bribes.Any() ? None : Some(e));
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