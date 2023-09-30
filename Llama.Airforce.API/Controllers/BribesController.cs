using LanguageExt;
using Llama.Airforce.Database.Contexts;
using Llama.Airforce.SeedWork.Types;
using Microsoft.AspNetCore.Mvc;
using Db = Llama.Airforce.Database.Models;

namespace Llama.Airforce.API.Controllers;

[ApiController]
[Route("[controller]")]
public class BribesController : ControllerBase
{
    private readonly BribesContext Context;
    private readonly BribesV2Context ContextV2;
    private readonly BribesV3Context ContextV3;

    public BribesController(
        BribesContext context,
        BribesV2Context contextV2,
        BribesV3Context contextV3)
    {
        Context = context;
        ContextV2 = contextV2;
        ContextV3 = contextV3;
    }

    public class IndexParams
    {
        public string? Platform { get; set; }
        public string? Protocol { get; set; }
        public string? Round { get; set; }
        public bool? L2 { get; set; }
    }

    [Route("")]
    [HttpPost]
    public async Task<ActionResult> Index([FromBody] IndexParams body)
    {
        var platform = string.IsNullOrWhiteSpace(body.Platform) ? "votium" : body.Platform;
        var protocol = string.IsNullOrWhiteSpace(body.Protocol) ? "cvx-crv" : body.Protocol;
        var l2 = body.L2 ?? false;

        var lastRoundV1 = await Context
            .Rounds(platform, protocol)
            .Map(rs => rs.LastOrDefault());

        var lastRoundV2 = await ContextV2
           .Rounds(platform, protocol)
           .Map(rs => rs.LastOrDefault());

        var lastRoundV3 = await ContextV3
           .Rounds(platform, protocol)
           .Map(rs => rs.LastOrDefault());

        var lastRound = l2
            ? lastRoundV3
            : new[] { lastRoundV1, lastRoundV2 }.Max();

        var hasRound = int.TryParse(body.Round, out var round);
        if (!hasRound || round > lastRound || round < 1)
            round = lastRound;

        var epochId = Db.Bribes.EpochId.Create(
            StringMax.Of(platform),
            StringMax.Of(protocol),
            round);

        ActionResult CreateResult<T>(Option<T> epoch) =>
            epoch.Match(
                Some: x => new JsonResult(new
                {
                    success = true,
                    epoch = x
                }),
                None: () => new JsonResult(new
                {
                    success = false
                }));

        // V1
        if (!l2 && round <= lastRoundV1)
        {
            var epoch = await Context
               .GetAsync(epochId)
               .MapT(epoch => (Models.Votium.Epoch)epoch);

            return CreateResult(epoch);
        }

        // V2
        if (!l2 && round <= lastRoundV2)
        {
            var epoch = await ContextV2
               .GetAsync(epochId)
               .MapT(epoch => (Models.Votium.EpochV2)epoch);

            return CreateResult(epoch);
        }

        // V3
        else
        {
            var epoch = await ContextV3
               .GetAsync(epochId)
               .MapT(epoch => (Models.Votium.EpochV3)epoch);

            return CreateResult(epoch);
        }
    }

    public class RoundsParams
    {
        public string? Platform { get; set; }
        public string? Protocol { get; set; }
    }

    [Route(nameof(Rounds))]
    [HttpPost]
    public async Task<ActionResult> Rounds([FromBody] RoundsParams body)
    {
        var platform = string.IsNullOrWhiteSpace(body.Platform) ? "votium" : body.Platform;
        var protocol = string.IsNullOrWhiteSpace(body.Protocol) ? "cvx-crv" : body.Protocol;

        var roundsV1 = await Context.Rounds(platform, protocol);
        var roundsV2 = await ContextV2.Rounds(platform, protocol);
        var roundsV3 = await ContextV3.Rounds(platform, protocol);

        var rounds = roundsV1.Concat(roundsV2).Concat(roundsV3).Distinct();

        return new JsonResult(new
        {
            rounds
        });
    }
}