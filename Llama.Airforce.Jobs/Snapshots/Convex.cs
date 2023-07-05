using System.Numerics;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Llama.Airforce.Jobs.Contracts;
using Llama.Airforce.Jobs.Snapshots.Models;
using Llama.Airforce.SeedWork.Extensions;
using Llama.Airforce.SeedWork.Types;
using Nethereum.Util;
using Newtonsoft.Json;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Snapshots;

public class Convex
{
    public const string SPACE_CVX = "cvx.eth";
    public const string SNAPSHOT_SCORE_URL = "https://score.snapshot.org/api/scores";

    public static Func<
            Func<HttpClient>,
            EitherAsync<Error, Map<string, (int Index, string Id)>>>
        GetProposalIds = fun((Func<HttpClient> httpFactory) => Snapshot.GetProposalIds
            .Par(httpFactory)
            .Par(SPACE_CVX)
            .Par(_ => true)
            .Par(id => (id.StartsWith("0x")
                    ? new Sha3Keccack().CalculateHashFromHex(id)
                    : new Sha3Keccack().CalculateHash(id))
                .Insert(0, "0x"))());

    /// <summary>
    /// Returns score for a list of voters at a certain block number.
    /// </summary>
    public static Func<
            Func<HttpClient>,
            Lst<Address>,
            BigInteger,
            EitherAsync<Error, Map<Address, double>>>
        GetScores = fun((
            Func<HttpClient> httpFactory,
            Lst<Address> voters,
            BigInteger block) =>
        {
            var address = (int)block switch
            {
                >= 15091880 => Address.Of("0x81768695e9fDdA232491bec5B21Fd1BC1116F917").ValueUnsafe().Value,
                >= 14400650 => Address.Of("0x1cc2CFe1d7e40bAb890Ca532AD0DBB413e072b988").ValueUnsafe().Value,
                >= 13948583 => Address.Of("0x59CcBAABBFCAC52E007A706242C5B81a48179BF2").ValueUnsafe().Value,
                _ => Addresses.Convex.Locked.Value
            };

            var cvxLockedStrategy = new Dictionary<string, dynamic>
            {
                { "name", "erc20-balance-of" },
                {
                    "params",
                    new
                    {
                        symbol = "CVX",
                        address,
                        decimals = 18
                    }
                }
            };

            var @params = new
            {
                space = SPACE_CVX,
                network = "1",
                snapshot = block,
                strategies = new List<dynamic> {
                    cvxLockedStrategy,
                    new Dictionary<string, dynamic>
                    {
                        { "name", "delegation" },
                        {
                            "params", new
                            {
                                symbol = "CVX",
                                strategies = new List<dynamic> { cvxLockedStrategy }
                            }
                        }
                    }
                },
                addresses = voters.Map(voter => voter.Value).ToList()
            };

            return Functions
                .HttpFunctions
                .PostData(
                    httpFactory,
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