using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Llama.Airforce.Database.Contexts;
using Llama.Airforce.SeedWork.Extensions;
using Llama.Airforce.SeedWork.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Llama.Airforce.API.Controllers;

[ApiController]
[Route("[controller]")]
public class AirdropController : ControllerBase
{
    private readonly AirdropContext Context;
    private readonly IMemoryCache Cache;

    public AirdropController(
        AirdropContext context,
        IMemoryCache cache)
    {
        Context = context;
        Cache = cache;
    }

    public class IndexParams
    {
        public string AirdropId { get; set; }
        public string Address { get; set; }
    }

    [HttpPost]
    public async Task<ActionResult> Index([FromBody] IndexParams body)
    {
        var address = Address.Of(body.Address).ValueUnsafe();
        var airdropId = string.IsNullOrWhiteSpace(body.AirdropId) ? "union" : body.AirdropId;

        // Use caching because Zapper drains our database.
        var airdrop = await Cache.GetOrCreateAsync(
            $"{airdropId}",
            cacheEntry =>
            {
                cacheEntry.SlidingExpiration = TimeSpan.FromDays(10);
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(14); // Two weeks per airdrop.
                return Context.GetAsync(airdropId);
            });

        var claim = airdrop.Bind(a => a
            .Claims
            .ToDictionary(x => Address.Of(x.Key).ValueUnsafe(), x => x.Value)
            .AsReadOnly()
            .TryGetValue(address));

        return claim.Match(
            Some: x => new JsonResult(new
            {
                success = true,
                claim = x
            }),
            None: () => new JsonResult(new
            {
                success = false
            }));
    }

    public class ClearCacheParams
    {
        public string Password { get; set; }
    }

    [Route(nameof(ClearCache))]
    [HttpPost]
    public ActionResult ClearCache([FromBody] ClearCacheParams body)
    {
        if (body.Password != "NotAnImportantPassword")
            return Content("Invalid password");

        Cache.Remove("union");
        Cache.Remove("ufxs");
        Cache.Remove("ucvx");

        return Content("Cache cleared (union, ufxs, ucvx)");
    }
}