using System.Numerics;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Llama.Airforce.Jobs.Extensions;
using Llama.Airforce.SeedWork.Types;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Web3;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Contracts;

public static class Votium
{
    #region Function Messages

    [Function("currentEpoch", "uint256")]
    private class CurrentEpochFunction : FunctionMessage { }

    [Function("withheldWeightAtEpoch", "uint256")]
    private class WithheldWeightAtEpochFunction : FunctionMessage
    {
        [Parameter("address", 1)]
        public string Gauge { get; set; } = "";

        [Parameter("uint256", 2)]
        public BigInteger Epoch { get; set; }
    }

    #endregion

    public static Func<IWeb3, Task<DateTime>> GetCurrentEpoch = fun((IWeb3 web3) => web3
        .Eth
        .GetContractQueryHandler<CurrentEpochFunction>()
        .QueryAsync<BigInteger>(Address.Of("0x63942E31E98f1833A234077f47880A66136a2D1e").ValueUnsafe(), new CurrentEpochFunction())
            .Map(x => DateTimeExt.FromUnixTimeSeconds((long)x)));

    public static Task<BigInteger> WithheldWeightAtEpoch(
        IWeb3 web3,
        Address registry,
        string gauge,
        BigInteger epoch) => web3
       .Eth
       .GetContractQueryHandler<WithheldWeightAtEpochFunction>()
       .QueryAsync<BigInteger>(registry.Value, new WithheldWeightAtEpochFunction
       {
           Gauge = gauge,
           Epoch = epoch
       });
}
