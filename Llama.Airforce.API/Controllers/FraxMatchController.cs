using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Llama.Airforce.Database.Contexts;
using Llama.Airforce.Database.Models.Bribes;
using Llama.Airforce.SeedWork.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using static LanguageExt.Prelude;

namespace Llama.Airforce.API.Controllers;

public class PoolFrax
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public class EpochFrax
{
    public string Platform { get; set; }
    public string Protocol { get; set; }
    public int Round { get; set; }

    public string Proposal { get; set; }
    public long End { get; set; }

    public double Native { get; set; }
    public double Frax { get; set; }
}

[ApiController]
[Route("[controller]")]
public class FraxMatchController : ControllerBase
{
    private readonly BribesContext Context;
    private readonly IMemoryCache Cache;

    public FraxMatchController(
        BribesContext context,
        IMemoryCache cache)
    {
        Context = context;
        Cache = cache;
    }

    private async Task<List<EpochId>> GetEpochs(StringMax platform, StringMax protocol)
    {
        var epochIds = await Cache.GetOrCreateAsync(
            "fraxmatch-epochs",
            cacheEntry =>
            {
                cacheEntry.SlidingExpiration = TimeSpan.FromHours(2);
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2); // Two weeks per airdrop.

                var epochIds = Context
                    .Rounds("votium", "cvx-crv")
                    .Map(rounds => rounds
                        .Map(round => EpochId.Create(platform, protocol, round))
                        .ToList());

                return epochIds;
            });

        return epochIds;
    }

    private async Task<Option<(Epoch Epoch, List<Bribe> Bribes)>> GetFraxMatches(EpochId epochId, Option<List<string>> poolIds)
    {
        var keyPoolIds = poolIds
            .Map(ids => string.Join('-', ids))
            .Match(id => $"-{id}", () => "");

        var key = $"fraxmatches-matches-{epochId.Round}{keyPoolIds}";

        var fraxMatches = await Cache.GetOrCreateAsync(
            key,
            cacheEntry =>
            {
                cacheEntry.SlidingExpiration = TimeSpan.FromHours(2);
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2); // Two weeks per airdrop.

                return Context.GetFraxMatches(epochId, poolIds);
            });

        return fraxMatches;
    }

    [Route(nameof(Pools))]
    [HttpGet]
    public async Task<ActionResult> Pools()
    {
        var platform = StringMax.Of("votium");
        var protocol = StringMax.Of("cvx-crv");
        var epochIds = await GetEpochs(platform, protocol);

        var pools = await epochIds
            .Map(epochId => GetFraxMatches(epochId, None))
            .SequenceSerial()
            .Map(epochs => epochs
                .Somes()
                .SelectMany(epoch => epoch.Bribes.Map(bribe => bribe.Pool))
                .Distinct()
                .Map(pool => new PoolFrax
                {
                    Id = pool,
                    Name = pool
                })
                .ToList());

        return new JsonResult(new
        {
            pools
        });
    }

    public class EpochsParams
    {
        public List<string> PoolIds { get; set; }
    }

    [Route(nameof(Epochs))]
    [HttpPost]
    public async Task<ActionResult> Epochs([FromBody] EpochsParams body)
    {
        var platform = StringMax.Of("votium");
        var protocol = StringMax.Of("cvx-crv");
        var epochIds = await GetEpochs(platform, protocol);

        var epochs = await epochIds
            .Map(epochId => GetFraxMatches(epochId, body.PoolIds))
            .SequenceSerial()
            .Map(epochs => epochs
                .Somes()
                .Map(epoch => new EpochFrax
                {
                    Platform = platform.ValueUnsafe().Value,
                    Protocol = protocol.ValueUnsafe().Value,
                    Round = epoch.Epoch.Round,
                    Proposal = epoch.Epoch.Proposal,
                    End = epoch.Epoch.End,
                    Native = epoch
                        .Bribes
                        .Where(bribe => !BribesContext.MatchTokens.Contains(bribe.Token.ToLowerInvariant()))
                        .Sum(bribe => bribe.AmountDollars),
                    Frax = epoch
                        .Bribes
                        .Where(bribe => BribesContext.MatchTokens.Contains(bribe.Token.ToLowerInvariant()))
                        .Sum(bribe => bribe.AmountDollars)
                })
                .ToList());

        return new JsonResult(new
        {
            epochs
        });
    }
}