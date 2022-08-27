using Llama.Airforce.Database.Contexts;
using Microsoft.AspNetCore.Mvc;

namespace Llama.Airforce.API.Controllers;

[ApiController]
[Route("[controller]")]
public class CurvePoolRatiosController : ControllerBase
{
    private readonly CurvePoolRatiosContext Context;

    public CurvePoolRatiosController(
        CurvePoolRatiosContext context)
    {
        Context = context;
    }

    [HttpGet]
    public async Task<ActionResult> Index()
    {
        var ratios = await Context.GetAllAsync();

        return new JsonResult(new
        {
            ratios = ratios
        });
    }
}