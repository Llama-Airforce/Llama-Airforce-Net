using System.Numerics;
using LanguageExt;
using Llama.Airforce.Jobs.Extensions;
using Llama.Airforce.Jobs.Functions;
using Llama.Airforce.SeedWork.Extensions;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Web3;
using static LanguageExt.Prelude;
using Error = LanguageExt.Common.Error;

namespace Llama.Airforce.Jobs.Contracts;

public static class Convex
{
    #region Function Messages

    [Function("boostedSupply", "uint256")]
    private class BoostedSupplyFunction : FunctionMessage { }

    [Function("findEpochId", "uint256")]
    private class FindEpochIdFunction : FunctionMessage
    {
        [Parameter("uint256", 1)]
        public BigInteger Time { get; set; }
    }

    [FunctionOutput]
    public class RewardDataOutput : IFunctionOutputDTO
    {
        [Parameter("bool", "useBoost", 1)]
        public bool UseBoost { get; set; }

        [Parameter("uint40", "periodFinish", 2)]
        public BigInteger PeriodFinish { get; set; }

        [Parameter("uint208", "rewardRate", 3)]
        public BigInteger RewardRate { get; set; }

        [Parameter("uint40", "lastUpdateTime", 4)]
        public BigInteger LastUpdateTime { get; set; }

        [Parameter("uint208", "rewardPerTokenStored", 5)]
        public BigInteger RewardPerTokenStored { get; set; }
    }

    [Function("rewardData")]
    private class RewardDataFunction : FunctionMessage
    {
        [Parameter("address", 1)]
        public string? Address { get; set; }
    }

    #endregion

    /// <summary>
    /// The day Convex got officially launched.
    /// </summary>
    public static DateTime Genesis = new(2021, 5, 17);

    public static int CvxDecimals = 18;
    public static int CurveDecimals = 18;

    public static Func<IWeb3, Task<BigInteger>> GetBoostedSupply = fun((IWeb3 web3) => web3
        .Eth
        .GetContractQueryHandler<BoostedSupplyFunction>()
        .QueryAsync<BigInteger>(Addresses.Convex.Locked2, new BoostedSupplyFunction()));

    public static Func<IWeb3, long, Task<BigInteger>> FindEpochId = fun((
        IWeb3 web3,
        long time) => web3
       .Eth
       .GetContractQueryHandler<FindEpochIdFunction>()
       .QueryAsync<BigInteger>(Addresses.Convex.Locked2, new FindEpochIdFunction
       {
           Time = time
       }));

    public static Func<IWeb3, Task<RewardDataOutput>> GetRewardRate = fun((IWeb3 web3) => web3
        .Eth
        .GetContractQueryHandler<RewardDataFunction>()
        .QueryDeserializingToObjectAsync<RewardDataOutput>(
            new RewardDataFunction()
            {
                Address = Addresses.CvxCrv.Token
            },
            Addresses.Convex.Locked2));

    /// <summary>
    /// Returns the APR of locking CVX tokens. This excludes any bribes.
    /// </summary>
    public static Func<
            Func<HttpClient>,
            IWeb3,
            EitherAsync<Error, double>>
        GetLockedApr = fun((
            Func<HttpClient> httpFactory,
            IWeb3 web3) =>
    {
        var cvxPrice_ = PriceFunctions.GetPrice(httpFactory, Addresses.Convex.Token, Network.Ethereum, Some(web3));
        var crvPrice_ = PriceFunctions.GetPrice(httpFactory, Addresses.Curve.Token, Network.Ethereum, Some(web3));
        var rate_ = GetRewardRate(web3).Map(x => x.RewardRate).DivideByDecimals(CvxDecimals).ToEitherAsync();
        var supply_ = GetBoostedSupply(web3).DivideByDecimals(CvxDecimals).ToEitherAsync();

        return from cvxPrice in cvxPrice_
               from crvPrice in crvPrice_
               from rate in rate_
               from supply in supply_
               select rate / (supply * cvxPrice) * 86400 * 365 * crvPrice;
    });

    /// <summary>
    /// Returns how much cvx would be minted today given a certain amount of CRV.
    /// </summary>
    public static Func<IWeb3, double, Task<double>> GetCvxMintAmount = fun(async (IWeb3 web3, double crvEarned) =>
    {
        const int CliffSize = 100_000;
        const int CliffCount = 1_000;
        const int MaxSupply = 100_000_000;

        // First get total supply.
        var cvxSupply = await ERC20
            .GetTotalSupply(web3, Addresses.Convex.Token)
            .DivideByDecimals(CvxDecimals);

        // Get current cliff.
        var currentCliff = cvxSupply / CliffSize;

        // If current cliff is under the max.
        if (currentCliff < CliffCount)
        {
            // Get remaining cliffs.
            var remaining = CliffCount - currentCliff;

            // Multiply ratio of remaining cliffs to total cliffs against amount CRV received.
            var cvxEarned = crvEarned * remaining / CliffCount;

            // Double check we have not gone over the max supply.
            var amountTillMax = MaxSupply - cvxSupply;

            if (cvxEarned > amountTillMax)
                cvxEarned = amountTillMax;

            return cvxEarned;
        }

        return 0;
    });

    /// <summary>
    /// Returns the APR for single-sided staking of cvxCRV.
    /// </summary>
    public static Func<
            Func<HttpClient>,
            IWeb3,
            EitherAsync<Error, double>>
        GetCvxCrvApr = fun((
            Func<HttpClient> httpFactory,
            IWeb3 web3) =>
    {
        var cvxPrice_ = PriceFunctions.GetPrice(httpFactory, Addresses.Convex.Token, Network.Ethereum, Some(web3));
        var crvPrice_ = PriceFunctions.GetPrice(httpFactory, Addresses.Curve.Token, Network.Ethereum, Some(web3));

        var rate_ = Curve.GetRewardRate(web3, Addresses.Curve.Staked).DivideByDecimals(CurveDecimals).ToEitherAsync();
        var threeRate_ = Curve.GetRewardRate(web3, Addresses.Curve.ThreePoolStaked).DivideByDecimals(CurveDecimals).ToEitherAsync();
        var supply_ = ERC20.GetTotalSupply(web3, Addresses.Curve.Staked).DivideByDecimals(CurveDecimals).ToEitherAsync();
        var virtualPrice_ = Curve.GetVirtualPrice(web3, Addresses.Curve.CurveSwap).DivideByDecimals(CurveDecimals).ToEitherAsync();

        var crvPerYear_ =
            from crvPrice in crvPrice_
            from rate in rate_
            from supply in supply_
            select rate / (supply * crvPrice) * 86400 * 365;

        var threePerYear_ =
            from crvPrice in crvPrice_
            from threeRate in threeRate_
            from supply in supply_
            select threeRate / (supply * crvPrice) * 86400 * 365;

        var cvxPerYear_ = crvPerYear_.MapAsync(x => GetCvxMintAmount(web3, x));

        return
            from crvPrice in crvPrice_
            from cvxPrice in cvxPrice_
            from crvPerYear in crvPerYear_
            from cvxPerYear in cvxPerYear_
            from threePerYear in threePerYear_
            from virtualPrice in virtualPrice_
            select crvPerYear * crvPrice + cvxPerYear * cvxPrice + threePerYear * virtualPrice;
    });

    /// <summary>
    /// Returns the total amount of CRV locked into cvxCRV tokens as USD.
    /// </summary>
    public static Func<
            Func<HttpClient>,
            IWeb3,
            EitherAsync<Error, double>>
        GetLockedCrvUsd = fun((
            Func<HttpClient> httpFactory,
            IWeb3 web3) => PriceFunctions
        .GetPrice(httpFactory, Addresses.Curve.Token, Network.Ethereum, Some(web3))
        .MapAsync(async crvPrice =>
        {
            var crvLocked = await ERC20.GetTotalSupply(web3, Addresses.CvxCrv.Token).DivideByDecimals(CurveDecimals);
            return crvLocked * crvPrice;
        }));
}
