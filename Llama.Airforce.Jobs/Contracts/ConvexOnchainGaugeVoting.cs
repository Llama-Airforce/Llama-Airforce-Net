using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Web3;
using Llama.Airforce.SeedWork.Types;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Contracts;

public static class ConvexOnchainGaugeVoting
{
    #region Function Messages

    [Function("gaugeTotal", "uint256")]
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
        [Parameter("uint48", "startTime", 1)]
        public BigInteger StartTime { get; set; }

        [Parameter("uint48", "endTime", 2)]
        public BigInteger EndTime { get; set; }

        [Parameter("uint48", "epoch", 3)]
        public BigInteger Epoch { get; set; }
    }

    [Function("proposals")]
    private class ProposalFunction : FunctionMessage
    {
        [Parameter("uint256", 1)]
        public BigInteger ProposalId { get; set; }
    }

    [Function("proposalCount", "uint256")]
    private class ProposalCountFunction : FunctionMessage { }

    [Function("getGaugeCount", "uint256")]
    private class GaugeCountFunction : FunctionMessage
    {
        [Parameter("uint256", 1)]
        public BigInteger ProposalId { get; set; }
    }

    [FunctionOutput]
    public class GaugeEntryOutput : IFunctionOutputDTO
    {
        [Parameter("address", "gauge", 1)]
        public string Gauge { get; set; }

        [Parameter("uint256", "totalWeight", 2)]
        public BigInteger TotalWeight { get; set; }
    }

    [Function("getGaugeEntry")]
    private class GaugeEntryFunction : FunctionMessage
    {
        [Parameter("uint256", 1)]
        public BigInteger ProposalId { get; set; }

        [Parameter("uint256", 2)]
        public BigInteger Index { get; set; }
    }

    [Function("getVoterCount", "uint256")]
    private class VoterCountFunction : FunctionMessage
    {
        [Parameter("uint256", 1)]
        public BigInteger ProposalId { get; set; }
    }

    [Function("getVoterAtIndex", "address")]
    private class VoterAtIndexFunction : FunctionMessage
    {
        [Parameter("uint256", 1)]
        public BigInteger ProposalId { get; set; }

        [Parameter("uint256", 2)]
        public BigInteger Index { get; set; }
    }

    [FunctionOutput]
    public class VoteOutput : IFunctionOutputDTO
    {
        [Parameter("address[]", "gauges", 1)]
        public List<string> Gauges { get; set; } = new();

        [Parameter("uint256[]", "weights", 2)]
        public List<BigInteger> Weights { get; set; } = new();

        [Parameter("bool", "voted", 3)]
        public bool Voted { get; set; }

        [Parameter("uint256", "baseWeight", 4)]
        public BigInteger BaseWeight { get; set; }

        [Parameter("int256", "adjustedWeight", 5)]
        public BigInteger AdjustedWeight { get; set; }
    }

    [Function("getVote")]
    private class VoteFunction : FunctionMessage
    {
        [Parameter("uint256", 1)]
        public BigInteger ProposalId { get; set; }

        [Parameter("address", 2)]
        public string User { get; set; }
    }

    [FunctionOutput]
    public class UserInfoOutput : IFunctionOutputDTO
    {
        [Parameter("uint96", "baseWeight", 1)]
        public BigInteger BaseWeight { get; set; }

        [Parameter("int96", "adjustedWeight", 2)]
        public BigInteger AdjustedWeight { get; set; }

        [Parameter("uint48", "lastVoteSyncNonce", 3)]
        public BigInteger LastVoteSyncNonce { get; set; }

        [Parameter("uint8", "voteStatus", 4)]
        public byte VoteStatus { get; set; }

        [Parameter("address", "delegate", 5)]
        public string Delegate { get; set; }

        [Parameter("uint96", "totalDelegationWeight", 6)]
        public BigInteger TotalDelegationWeight { get; set; }
    }

    [Function("userInfo")]
    private class UserInfoFunction : FunctionMessage
    {
        [Parameter("uint256", 1)]
        public BigInteger ProposalId { get; set; }

        [Parameter("address", 2)]
        public string User { get; set; }
    }

    [Function("getContributingWeights", "uint256[]")]
    private class ContributingWeightsFunction : FunctionMessage
    {
        [Parameter("uint256", 1)]
        public BigInteger ProposalId { get; set; }

        [Parameter("address", 2)]
        public string Delegate { get; set; }

        [Parameter("address[]", 3)]
        public List<string> Users { get; set; } = new();

        [Parameter("address", 4)]
        public string GaugePlatform { get; set; }
    }

    #endregion

    public static Func<IWeb3, Address, Task<BigInteger>> ProposalCount = fun((
        IWeb3 web3,
        Address platform) => web3
       .Eth
       .GetContractQueryHandler<ProposalCountFunction>()
       .QueryAsync<BigInteger>(platform.Value, new ProposalCountFunction()));

    public static Func<IWeb3, Address, BigInteger, Task<ProposalOutput>> GetProposal = fun((
        IWeb3 web3,
        Address platform,
        BigInteger proposalId) => web3
       .Eth
       .GetContractQueryHandler<ProposalFunction>()
       .QueryDeserializingToObjectAsync<ProposalOutput>(
            new ProposalFunction
            {
                ProposalId = proposalId
            },
            platform.Value));

    public static Func<IWeb3, Address, BigInteger, Task<BigInteger>> VoteTotal = fun((
        IWeb3 web3,
        Address platform,
        BigInteger proposalId) => web3
       .Eth
       .GetContractQueryHandler<VoteTotalFunction>()
       .QueryAsync<BigInteger>(platform.Value, new VoteTotalFunction
       {
           ProposalId = proposalId,
       }));

    public static Func<IWeb3, Address, BigInteger, string, Task<BigInteger>> GaugeTotal = fun((
        IWeb3 web3,
        Address platform,
        BigInteger proposalId,
        string gauge) => web3
       .Eth
       .GetContractQueryHandler<GaugeTotalFunction>()
       .QueryAsync<BigInteger>(platform.Value, new GaugeTotalFunction
       {
           ProposalId = proposalId,
           Gauge = gauge
       }));

    public static Func<IWeb3, Address, BigInteger, Task<BigInteger>> GaugeCount = fun((
        IWeb3 web3,
        Address platform,
        BigInteger proposalId) => web3
       .Eth
       .GetContractQueryHandler<GaugeCountFunction>()
       .QueryAsync<BigInteger>(platform.Value, new GaugeCountFunction
       {
           ProposalId = proposalId
       }));

    public static Func<IWeb3, Address, BigInteger, BigInteger, Task<GaugeEntryOutput>> GetGaugeEntry = fun((
        IWeb3 web3,
        Address platform,
        BigInteger proposalId,
        BigInteger index) => web3
       .Eth
       .GetContractQueryHandler<GaugeEntryFunction>()
       .QueryDeserializingToObjectAsync<GaugeEntryOutput>(
            new GaugeEntryFunction
            {
                ProposalId = proposalId,
                Index = index
            },
            platform.Value));

    public static Func<IWeb3, Address, BigInteger, Task<BigInteger>> VoterCount = fun((
        IWeb3 web3,
        Address platform,
        BigInteger proposalId) => web3
       .Eth
       .GetContractQueryHandler<VoterCountFunction>()
       .QueryAsync<BigInteger>(platform.Value, new VoterCountFunction
       {
           ProposalId = proposalId
       }));

    public static Func<IWeb3, Address, BigInteger, BigInteger, Task<string>> GetVoterAtIndex = fun((
        IWeb3 web3,
        Address platform,
        BigInteger proposalId,
        BigInteger index) => web3
       .Eth
       .GetContractQueryHandler<VoterAtIndexFunction>()
       .QueryAsync<string>(platform.Value, new VoterAtIndexFunction
       {
           ProposalId = proposalId,
           Index = index
       }));

    public static Func<IWeb3, Address, BigInteger, string, Task<VoteOutput>> GetVote = fun((
        IWeb3 web3,
        Address platform,
        BigInteger proposalId,
        string user) => web3
       .Eth
       .GetContractQueryHandler<VoteFunction>()
       .QueryDeserializingToObjectAsync<VoteOutput>(
            new VoteFunction
            {
                ProposalId = proposalId,
                User = user
            },
            platform.Value));

    public static Func<IWeb3, Address, BigInteger, string, Task<UserInfoOutput>> GetUserInfo = fun((
        IWeb3 web3,
        Address platform,
        BigInteger proposalId,
        string user) => web3
       .Eth
       .GetContractQueryHandler<UserInfoFunction>()
       .QueryDeserializingToObjectAsync<UserInfoOutput>(
            new UserInfoFunction
            {
                ProposalId = proposalId,
                User = user
            },
            platform.Value));

    public static Func<IWeb3, BigInteger, string, List<string>, Address, Task<List<BigInteger>>> GetContributingWeights = fun((
        IWeb3 web3,
        BigInteger proposalId,
        string @delegate,
        List<string> users,
        Address platform) => web3
       .Eth
       .GetContractQueryHandler<ContributingWeightsFunction>()
       .QueryAsync<List<BigInteger>>(
            Addresses.Convex.GaugeVoteHelper.Value,
            new ContributingWeightsFunction
            {
                ProposalId = proposalId,
                Delegate = @delegate,
                Users = users,
                GaugePlatform = platform.Value
            }));
}
