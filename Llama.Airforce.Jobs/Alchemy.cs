using System.Text;
using LanguageExt;
using LanguageExt.Common;
using Llama.Airforce.Jobs.Contracts;
using Llama.Airforce.Jobs.Subgraphs.Models;
using Llama.Airforce.SeedWork.Extensions;
using Llama.Airforce.SeedWork.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs;

public static class Alchemy
{
    /// <summary>
    /// Returns the latest block number.
    /// </summary>
    public static Func<string, EitherAsync<Error, int>> GetCurrentBlock = fun((string alchemy) =>
        TryAsync(async () =>
        {
            using var httpClient = new HttpClient();

            var reqParams = new
            {
                jsonrpc = "2.0",
                id = 0,
                method = "eth_blockNumber",
                @params = new[] { new { } }
            };
            var jsonReqParams = JsonConvert.SerializeObject(reqParams);
            var resp = await httpClient.PostAsync(
                alchemy,
                new StringContent(jsonReqParams, Encoding.UTF8, "application/json"));

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Unable to get most recent block number, status code: {resp.StatusCode}");

            var content = await resp.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            return fun(() => (string)json["result"])
                .ParseHexInt()
                .Match(
                    x => x,
                    _ => throw new Exception("Failed to parse block number value"));
        })
            .ToEither());

    /// <summary>
    /// Returns all transfers of a pool's tokens to a single admin address.
    /// </summary>
    public static Func<
            string,
            int,
            Address,
            CurvePool,
            EitherAsync<Error, Lst<Curve.AdminTransfer>>>
        GetTransfersToAdmin = fun((
            string alchemy,
            int endBlock,
            Address adminWallet,
            CurvePool pool) =>
        TryAsync(async () =>
        {
            using var httpClient = new HttpClient();

            var reqParams = new
            {
                jsonrpc = "2.0",
                id = 0,
                method = "alchemy_getAssetTransfers",
                @params = new[]
                {
                    new
                    {
                        fromBlock = "0x" + 12667823.ToString("x"), // registry deployment block
                        toBlock = "0x" + endBlock.ToString("x"),
                        fromAddress = pool.Swap,
                        toAddress = adminWallet.ToString(),
                        contractAddresses = pool.CoinList,
                        maxCount = "0x3e8",
                        excludeZeroValue = true,
                        category = new[]
                        {
                            "external",
                            "erc20",
                            "internal"
                        }
                    }
                }
            };

            var jsonReqParams = JsonConvert.SerializeObject(reqParams);
            var resp = await httpClient.PostAsync(
                alchemy,
                new StringContent(jsonReqParams, Encoding.UTF8, "application/json"));

            if (!resp.IsSuccessStatusCode)
                throw new Exception(
                    $"Unable to get transfer data for pool {pool.Name}, status code: {resp.StatusCode}");

            var content = await resp.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            return toList(json["result"]["transfers"])
                .Map(x =>
                {
                    var block = fun(() => (string)x["blockNum"])
                        .ParseHexInt()
                        .IfFailThrow();

                    var value = fun(() => (string)x["value"])
                        .ParseDouble()
                        .BindFail(_ => fun(() => (string)x["rawContract"]["value"])
                            .ParseHexDouble()
                            .Map(y => y / Math.Pow(10, 18)))
                        .IfFailThrow();

                    var token = (string)x["asset"];

                    return new Curve.AdminTransfer(
                        block: block,
                        value: value,
                        token: token);
                });
        })
            .ToEither());

    /// <summary>
    /// Returns all transfers of a pool's tokens to the admin wallets.
    /// </summary>
    public static Func<
            string,
            CurvePool,
            EitherAsync<Error, Lst<Curve.AdminTransfer>>>
        GetTransfers = fun((
            string alchemy,
            CurvePool pool) =>
        {
            var adminWallets = new[] { Addresses.Curve.StableSwapProxy, Addresses.Curve.FeeDistributor };

            return GetCurrentBlock(alchemy)
                .Bind(currentBlock => adminWallets
                    .Map(wallet => GetTransfersToAdmin(alchemy, currentBlock, wallet, pool))
                    .SequenceSerial())
                .Map(x => x.SelectMany(x => x).toList());
        });

    /// <summary>
    /// Matches an ERC20 transfer to a snapshot by block.
    /// </summary>
    public static Func<
            List<FeeSnapshot>,
            Curve.AdminTransfer,
            Option<Curve.Fees>>
        GetFeesFromTransfer = fun((
            List<FeeSnapshot> snapshots,
            Curve.AdminTransfer adminTransfer) =>
        {
            // We select the index of the snapshot with the closest block to retrieve timestamp
            var closestSnapshot = snapshots.MinBy(x => Math.Abs(adminTransfer.Block - x.Block));

            return closestSnapshot != null
                ? Some(new Curve.Fees(
                    closestSnapshot.TimeStamp,
                    adminTransfer.Value * closestSnapshot.PoolTokenPrice))
                : None;
        });
}