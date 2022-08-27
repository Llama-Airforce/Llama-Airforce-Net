using Llama.Airforce.Database.Contexts;
using Microsoft.AspNetCore.Mvc;

namespace Llama.Airforce.API.Controllers;

[ApiController]
[Route("[controller]")]
public class CurvePoolSnapshotsController : ControllerBase
{
    private readonly CurvePoolSnapshotsContext Context;

    public CurvePoolSnapshotsController(
        CurvePoolSnapshotsContext context)
    {
        Context = context;
    }

    public class IndexParams
    {
        public string Pool { get; set; }
    }

    [HttpPost]
    public async Task<ActionResult> Index([FromBody] IndexParams body)
    {
        var data = await Context.GetAsync(body.Pool);

        return data.Match(
            Some: i => new JsonResult(new
            {
                success = true,
                data = i
            }),
            None: () => new JsonResult(new
            {
                success = false
            }));
    }
}