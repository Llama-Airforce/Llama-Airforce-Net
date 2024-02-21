using System.Numerics;
using Llama.Airforce.SeedWork.Types;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Util;
using Nethereum.Web3;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Contracts;

public static class Prisma
{
    #region Function Messages

    [Function("getWeek", "uint256")]
    private class GetWeekFunction : FunctionMessage { }

    [Function("weeklyEmissions", "uint128")]
    private class GetWeeklyEmissionsFunction : FunctionMessage
    {
        [Parameter("uint256", 1)]
        public BigInteger week { get; set; }
    }

    [Function("getTotalWeight", "uint256")]
    private class GetTotalWeightFunction : FunctionMessage { }

    [Function("getAccountWeight", "uint256")]
    private class GetAccountWeightFunction : FunctionMessage
    {
        [Parameter("address", 1)]
        public string Address { get; set; }
    }

    #endregion

    public static Func<IWeb3, Task<BigInteger>> GetWeek = fun((IWeb3 web3) => web3
        .Eth
        .GetContractQueryHandler<GetWeekFunction>()
        .QueryAsync<BigInteger>(Addresses.Prisma.Vault, new GetWeekFunction()));

    public static Func<IWeb3, BigInteger, Task<BigInteger>> GetWeeklyEmissions = fun((IWeb3 web3, BigInteger week) => web3
        .Eth
        .GetContractQueryHandler<GetWeeklyEmissionsFunction>()
        .QueryAsync<BigInteger>(Addresses.Prisma.Vault, new GetWeeklyEmissionsFunction
        {
            week = week
        }));

    public static Func<IWeb3, Task<BigInteger>> GetTotalWeight = fun((IWeb3 web3) => web3
       .Eth
       .GetContractQueryHandler<GetTotalWeightFunction>()
       .QueryAsync<BigInteger>(Addresses.Prisma.Locker, new GetTotalWeightFunction()));

    public static Func<IWeb3, Address, Task<BigInteger>> GetAccountWeight = fun((IWeb3 web3, Address address) => web3
       .Eth
       .GetContractQueryHandler<GetAccountWeightFunction>()
       .QueryAsync<BigInteger>(Addresses.Prisma.Locker, new GetAccountWeightFunction
        {
           Address = address
        }));

    /// <summary>
    /// Gets the voting power of an address as a number between [0, 1].
    /// </summary>
    public static Func<IWeb3, Address, Task<double>> GetVotingPower = fun(async (IWeb3 web3, Address owner) =>
    {
        // Get the amount of vePRISMA the owner holds.
        var balanceOf = await GetAccountWeight(web3, owner);

        // Get the total amount of voting power in the gauge controller.
        var totalWeight = await GetTotalWeight(web3);

        // Calculate the relative power of the owner compared to the total supply of vePRISMA.
        return (double)(new BigDecimal(balanceOf) / new BigDecimal(totalWeight));
    });
}