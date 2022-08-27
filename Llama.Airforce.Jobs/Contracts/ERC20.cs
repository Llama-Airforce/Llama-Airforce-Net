using System.Numerics;
using Llama.Airforce.SeedWork.Types;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Web3;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Contracts;

public static class ERC20
{
    #region Function Messages

    [Function("symbol", "string")]
    private class SymbolFunction : FunctionMessage { }

    [Function("totalSupply", "uint256")]
    private class TotalSupplyFunction : FunctionMessage { }

    [Function("lockedSupply", "uint256")]
    private class LockedSupplyFunction : FunctionMessage { }

    [Function("decimals", "uint256")]
    private class DecimalsFunction : FunctionMessage { }

    [Function("balanceOf", "uint256")]
    private class BalanceOfFunction : FunctionMessage
    {
        [Parameter("address", 1)]
        public string? Address { get; set; }
    }

    #endregion

    public static Func<IWeb3, Address, Task<string>> GetSymbol = fun((IWeb3 web3, Address erc20) => web3
        .Eth
        .GetContractQueryHandler<SymbolFunction>()
        .QueryAsync<string>(erc20, new SymbolFunction()));

    public static Func<IWeb3, Address, Task<BigInteger>> GetTotalSupply = fun((IWeb3 web3, Address erc20) => web3
        .Eth
        .GetContractQueryHandler<TotalSupplyFunction>()
        .QueryAsync<BigInteger>(erc20, new TotalSupplyFunction()));

    public static Func<IWeb3, Address, Task<BigInteger>> GetLockedSupply = fun((IWeb3 web3, Address erc20) => web3
        .Eth
        .GetContractQueryHandler<LockedSupplyFunction>()
        .QueryAsync<BigInteger>(erc20, new LockedSupplyFunction()));

    public static Func<IWeb3, Address, Task<BigInteger>> GetDecimals = fun((IWeb3 web3, Address erc20) => web3
        .Eth
        .GetContractQueryHandler<DecimalsFunction>()
        .QueryAsync<BigInteger>(erc20, new DecimalsFunction()));

    public static Func<
            IWeb3,
            Address,
            Address,
            Task<BigInteger>>
        GetBalanceOf = fun((
            IWeb3 web3,
            Address erc20,
            Address owner) => web3
        .Eth
        .GetContractQueryHandler<BalanceOfFunction>()
        .QueryAsync<BigInteger>(erc20, new BalanceOfFunction()
        {
            Address = owner
        }));
}