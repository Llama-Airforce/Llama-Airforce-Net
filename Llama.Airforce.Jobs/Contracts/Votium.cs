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

    #endregion

    public static Func<IWeb3, Task<DateTime>> GetCurrentEpoch = fun((IWeb3 web3) => web3
        .Eth
        .GetContractQueryHandler<CurrentEpochFunction>()
        .QueryAsync<BigInteger>(Address.Of("0x63942E31E98f1833A234077f47880A66136a2D1e").ValueUnsafe(), new CurrentEpochFunction())
            .Map(x => DateTimeExt.FromUnixTimeSeconds((long)x)));
}