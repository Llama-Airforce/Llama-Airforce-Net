using System;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Hex.HexTypes;
using System.Numerics;

class Program
{
    private static string contractAddress = "0x365AccFCa291e7D3914637ABf1F7635dB165Bb09"; //FXN TOKEN
    private static string providerUrl = "";  //FILL PROVIDER URI

    private static string abi = @"[
        {
            ""stateMutability"": ""view"",
            ""type"": ""function"",
            ""name"": ""mintable_in_timeframe"",
            ""inputs"": [
                { ""name"": ""start"", ""type"": ""uint256"" },
                { ""name"": ""end"", ""type"": ""uint256"" }
            ],
            ""outputs"": [
                { ""name"": """", ""type"": ""uint256"" }
            ]
        }
    ]";

    static async Task Main(string[] args)
    {
        var web3 = new Web3(providerUrl);

        // Dates for fetching
        string startDate = "2024-11-02";
        string endDate = "2024-11-03";

        try 
        {
            var tokensMinted = await GetMintedTokensInTimeframe(web3, startDate, endDate);
            Console.WriteLine($"Tokens minted from {startDate} to {endDate}: {tokensMinted}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public static async Task<BigInteger> GetMintedTokensInTimeframe(Web3 web3, string startDate, string endDate)
    {
        DateTime startDateTime = DateTime.Parse(startDate).ToUniversalTime();
        DateTime endDateTime = DateTime.Parse(endDate).ToUniversalTime();

        // Convert to Unix timestamp
        long startUnix = ((DateTimeOffset)startDateTime).ToUnixTimeSeconds();
        long endUnix = ((DateTimeOffset)endDateTime).ToUnixTimeSeconds();

        // Convert to BigInteger first
        BigInteger startBigInt = new BigInteger(startUnix);
        BigInteger endBigInt = new BigInteger(endUnix);

        Console.WriteLine($"Debug - Start timestamp: {startUnix}");
        Console.WriteLine($"Debug - End timestamp: {endUnix}");

        var contract = web3.Eth.GetContract(abi, contractAddress);
        var mintableInTimeframeFunction = contract.GetFunction("mintable_in_timeframe");

        try 
        {
            object[] parameters = new object[] 
            { 
                startBigInt,
                endBigInt
            };

            return await mintableInTimeframeFunction.CallAsync<BigInteger>(parameters);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Contract call error: {ex.Message}");
            throw;
        }
    }
}