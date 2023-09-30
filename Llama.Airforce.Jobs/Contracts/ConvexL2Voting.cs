using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Web3;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Contracts;

public static class ConvexL2Voting
{
    #region Function Messages

    [Function("gaugeTotals", "uint256")]
    private class GaugeTotalFunction : FunctionMessage
    {
        [Parameter("uint256", 1)]
        public BigInteger ProposalId { get; set; }

        [Parameter("address", 2)]
        public string Gauge { get; set; }
    }

    [Function("voteTotals", "uint256")]
    private class VoteTotalFunction : FunctionMessage
    {
        [Parameter("uint256", 1)]
        public BigInteger ProposalId { get; set; }
    }

    [FunctionOutput]
    public class ProposalOutput : IFunctionOutputDTO
    {
        [Parameter("bytes32", "baseWeightMerkleRoot", 1)]
        public byte[] BaseWeightMerkleRoot { get; set; }

        [Parameter("uint256 ", "startTime", 2)]
        public BigInteger StartTime { get; set; }

        [Parameter("uint256 ", "endTime", 3)]
        public BigInteger EndTime { get; set; }
    }

    [Function("proposals")]
    private class ProposalFunction : FunctionMessage
    {
        [Parameter("uint256", 1)]
        public BigInteger ProposalId { get; set; }
    }

    #endregion

    public static Func<
            IWeb3,
            BigInteger,
            string,
            Task<BigInteger>> 
        GaugeTotal = fun((
            IWeb3 web3,
            BigInteger proposalId,
            string gauge) => web3
        .Eth
        .GetContractQueryHandler<GaugeTotalFunction>()
        .QueryAsync<BigInteger>(Addresses.Convex.L2GaugeVotingPlatform, new GaugeTotalFunction
        {
            ProposalId = proposalId,
            Gauge = gauge
        }));

    public static Func<
            IWeb3,
            BigInteger,
            Task<BigInteger>>
        VoteTotal = fun((
            IWeb3 web3,
            BigInteger proposalId) => web3
       .Eth
       .GetContractQueryHandler<VoteTotalFunction>()
       .QueryAsync<BigInteger>(Addresses.Convex.L2GaugeVotingPlatform, new VoteTotalFunction
       {
            ProposalId = proposalId,
        }));

    public static Func<
            IWeb3,
            BigInteger,
            Task<ProposalOutput>>
        GetProposal = fun((
            IWeb3 web3,
            BigInteger proposalId) => web3
       .Eth
       .GetContractQueryHandler<ProposalFunction>()
       .QueryDeserializingToObjectAsync<ProposalOutput>(
            new ProposalFunction
            {
                ProposalId = proposalId
            },
            Addresses.Convex.L2GaugeVotingPlatform));
}