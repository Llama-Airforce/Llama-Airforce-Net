using System.Numerics;
using Llama.Airforce.SeedWork.Types;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Web3;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Contracts;

public static class UniV3
{
    #region Function Messages

    [FunctionOutput]
    public class Slot0Output : FunctionMessage
    {
        [Parameter("uint160", "sqrtPriceX96")]
        public BigInteger SqrtPriceX96 { get; set; }
    }

    #endregion

    public static Func<IWeb3, Address, Task<Slot0Output>> GetSlot0 = fun(async (IWeb3 web3, Address address) =>
    {
        var abi = @"[{""inputs"":[],""name"":""slot0"",""outputs"":[{""internalType"":""uint160"",""name"":""sqrtPriceX96"",""type"":""uint160""},{""internalType"":""int24"",""name"":""tick"",""type"":""int24""},{""internalType"":""uint16"",""name"":""observationIndex"",""type"":""uint16""},{""internalType"":""uint16"",""name"":""observationCardinality"",""type"":""uint16""},{""internalType"":""uint16"",""name"":""observationCardinalityNext"",""type"":""uint16""},{""internalType"":""uint8"",""name"":""feeProtocol"",""type"":""uint8""},{""internalType"":""bool"",""name"":""unlocked"",""type"":""bool""}],""stateMutability"":""view"",""type"":""function""}]";

        var pool = web3.Eth.GetContract(abi, address);
        var slotFunc = pool.GetFunction("slot0");
        var slot0 = await slotFunc.CallAsync<Slot0Output>();

        return slot0;
    });
}