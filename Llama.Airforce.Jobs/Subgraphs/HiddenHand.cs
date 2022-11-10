using LanguageExt;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Llama.Airforce.Jobs.Contracts;
using Llama.Airforce.Jobs.Subgraphs.Models;
using Llama.Airforce.SeedWork.Extensions;
using Llama.Airforce.SeedWork.Types;
using Nethereum.ABI;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;
using static LanguageExt.Prelude;
using Dom = Llama.Airforce.Domain.Models;

namespace Llama.Airforce.Jobs.Subgraphs;

public class HiddenHand
{
    public const string SUBGRAPH_URL_HIDDENHAND = "https://api.thegraph.com/subgraphs/name/convex-community/hidden-hand";

    /// <summary>
    /// Returns Votium epoch & bribe history from The Graph
    /// </summary>
    public static Func<
            List<(int Index, string Id)>,
            EitherAsync<Error, Lst<Dom.Epoch>>>
        GetEpochs = fun((
            List<(int Index, string Id)> snapshotProposalIds) =>
        snapshotProposalIds
            .Map(x =>
            {
                var (proposalIndex, proposalId) = x;

                // HiddenHand messed something up and had to restart the proposal.
                // Because of this, the index we calculate is too high, so we need to reduce it.
                if (proposalIndex == 27)
                    proposalIndex--;

                // Generate a mapping for each choice and the corresponding HH proposal id for the subgraph.
                var choices_ = Snapshots
                    .Snapshot
                    .GetNumChoices(proposalId)
                    .Map(numChoices => toMap(Enumerable
                        .Range(0, numChoices)
                        .Select(i => (
                            Choice: i,
                            Id: new ABIEncode()
                                .GetSha3ABIEncodedPacked(proposalIndex, i)
                                .ToHex()
                                .Insert(0, "0x")))));

                // Fetch the bribes for each bribe (proposal).
                var proposals_ = choices_.Bind(choices =>
                {
                    var query = $@"{{
proposals(
    where: {{
      id_in: [{string.Join(",", choices.Values.Select(id => $"\"{id}\""))}]
    }}
    first: 1000
  ) {{
    id
    bribes {{
      token
      amount
  }}
}} }}";

                    return Subgraph.GetData(SUBGRAPH_URL_HIDDENHAND, query)
                        .MapTry(JsonConvert.DeserializeObject<RequestEpochsAura>)
                        .MapTry(data =>
                            choices.Map((_, id) => data.Data.ProposalList.Single(proposal => proposal.Id == id)));
                });

                // Generate domain models from the subgraph data.
                var epoch_ = proposals_.Map(proposals =>
                {
                    var bribes = proposals
                        .SelectMany(x => x.Value.Bribes
                            .Select(bribe =>
                            {
                                // Bribe with ETH have their token set to Aura's bribe vault address.
                                // We'll convert it to the WETH address.
                                var tokenAddress = Address.Of(bribe.Token).ValueUnsafe();
                                var token = Addresses.HiddenHand.AuraBribeVault.Equals(tokenAddress)
                                    ? Addresses.ERC20.WETH
                                    : tokenAddress;

                                return new Dom.Bribe(x.Key, token, bribe.Amount);
                            }))
                        .ToList();

                    return new Dom.Epoch(proposalId, 0, bribes);
                });

                return epoch_;
            })
            .SequenceSerial()
            // Filter out epochs with no bribes.
            .Map(epochs => toList(epochs.Where(epoch => epoch.Bribes.Any()))));
}