using System.Threading.Tasks;
using LanguageExt;
using Llama.Airforce.Database.Models.Airforce;
using Microsoft.Azure.Cosmos;

namespace Llama.Airforce.Database.Contexts;

public class AirdropContext
{
    private readonly Container Container;

    public AirdropContext(
        CosmosClient dbClient,
        string dbName,
        string containerName)
    {
        Container = dbClient.GetContainer(dbName, containerName);
    }

    public async Task<Option<Airdrop>> GetAsync(string airdropId)
    {
        try
        {
            var resp = await Container.ReadItemAsync<Airdrop>(airdropId, new PartitionKey(airdropId));
            return resp.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Option<Airdrop>.None;
        }
    }

    public static async Task<AirdropContext> Create(
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

        var containerName = "Airdrops";
        var database = await dbClient.CreateDatabaseIfNotExistsAsync(dbName);
        await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

        return new AirdropContext(
            dbClient: dbClient,
            dbName: dbName,
            containerName: containerName);
    }
}