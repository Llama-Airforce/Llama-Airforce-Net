using System.Numerics;
using LanguageExt;
using LanguageExt.Common;
using Llama.Airforce.Jobs.Contracts;
using Llama.Airforce.Jobs.Snapshots.Models;
using Llama.Airforce.SeedWork.Extensions;
using Llama.Airforce.SeedWork.Types;
using Newtonsoft.Json;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Snapshots;

public class Aura
{
    public const string SPACE_AURA = "aurafinance.eth";
    public const string SNAPSHOT_SCORE_URL = "https://score.snapshot.org/api/scores";

    public static Func<EitherAsync<Error, Map<string, (int Index, string Id)>>> GetProposalIds = Snapshot.GetProposalIds
        .Par(SPACE_AURA)
        .Par(proposal => proposal.Title.ToLowerInvariant().StartsWith("gauge weight for week of"))
        .Par(id => id);

    /// <summary>
    /// Returns score for a list of voters at a certain block number.
    /// </summary>
    public static Func<
            Lst<Address>,
            BigInteger,
            EitherAsync<Error, Map<Address, double>>>
        GetScores = fun((
            Lst<Address> voters,
            BigInteger block) =>
        {
            var address = Addresses.Aura.Locked.Value;

            var auraLockedStrategy = new Dictionary<string, dynamic>
            {
                { "name", "erc20-votes-with-override" },
                {
                    "params",
                    new
                    {
                        symbol = "vlAURA",
                        address,
                        decimals = 18,
                        isSnapshotDelegatedScore = false,
                        includeSnapshotDelegations = false
                    }
                }
            };

            var @params = new
            {
                space = SPACE_AURA,
                network = "1",
                snapshot = block,
                strategies = new List<dynamic> {
                    auraLockedStrategy,
                    new Dictionary<string, dynamic>
                    {
                        { "name", "delegation" },
                        {
                            "params", new
                            {
                                symbol = "vlAURA",
                                strategies = new List<dynamic> { auraLockedStrategy }
                            }
                        }
                    }
                },
                addresses = voters.Map(voter => voter.Value).ToList()
            };

            return Functions
                .HttpFunctions
                .GetData(
                    SNAPSHOT_SCORE_URL,
                    JsonConvert.SerializeObject(new Dictionary<string, dynamic> { { "params", @params } }))
                .MapTry(JsonConvert.DeserializeObject<RequestScores>)
                // Parse results and sum the strategies for each address.
                .MapTry(x => x.Result.Scores.Aggregate(Map<Address, double>(), (acc, strategy) =>
                {
                    foreach (var (address, score) in strategy)
                        acc = acc.AddOrUpdate(Address.Of(address), x => x + score, score);

                    return acc;
                }));
        });
}