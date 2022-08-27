using LanguageExt;
using LanguageExt.UnsafeValueAccess;
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

    public BribesController(
        BribesContext context)
    {
        Context = context;
    }

    public class IndexParams
    {
        public string? Platform { get; set; }
        public string? Protocol { get; set; }
        public string? Round { get; set; }
    }

    [Route("")]
    [HttpPost]
    public async Task<ActionResult> Index([FromBody] IndexParams body)
    {
        var platform = string.IsNullOrWhiteSpace(body.Platform) ? "votium" : body.Platform;
        var protocol = string.IsNullOrWhiteSpace(body.Protocol) ? "cvx-crv" : body.Protocol;

        var lastRound = await Context
            .Rounds(body.Platform, body.Protocol)
            .Map(rs => rs.LastOrDefault());

        var hasRound = int.TryParse(body.Round, out var round);
        if (!hasRound || round > lastRound || round < 1)
            round = lastRound;

        var epochId = Db.Bribes.EpochId.Create(
            StringMax.Of(platform).ValueUnsafe(),
            StringMax.Of(protocol).ValueUnsafe(),
            round);

        var epoch = await Context
            .GetAsync(epochId)
            .MapT(epoch => (Models.Votium.Epoch)epoch);

        return epoch.Match(
            Some: x => new JsonResult(new
            {
                success = true,
                epoch = x
            }),
            None: () => new JsonResult(new
            {
                success = false
            }));
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

        var rounds = await Context.Rounds(platform, protocol);

        return new JsonResult(new
        {
            rounds
        });
    }
}