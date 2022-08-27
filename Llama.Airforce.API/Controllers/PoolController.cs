using Llama.Airforce.Database.Contexts;
using Microsoft.AspNetCore.Mvc;

namespace Llama.Airforce.API.Controllers;

[ApiController]
[Route("[controller]")]
public class PoolController : ControllerBase
{
    private readonly PoolContext Context;

    public PoolController(
        PoolContext context)
    {
        Context = context;
    }

    [HttpGet]
    public async Task<ActionResult> Index()
    {
        var pools = await Context.GetAllAsync();

        return new JsonResult(new
        {
            pools = pools
        });
    }
}