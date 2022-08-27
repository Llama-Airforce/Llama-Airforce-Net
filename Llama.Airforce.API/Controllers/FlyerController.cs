using LanguageExt;
using Llama.Airforce.Database.Contexts;
using Microsoft.AspNetCore.Mvc;
using Db = Llama.Airforce.Database.Models;

namespace Llama.Airforce.API.Controllers;

/// <summary>
/// This controller is deprecated for the dashboard controller.
/// It exists merely for backwards compatibility and 3rd parties
/// that depend on this API's URL existence.
/// </summary>
[ApiController]
[Route("[controller]")]
public class FlyerController : ControllerBase
{
    private readonly DashboardContext Context;

    public FlyerController(
        DashboardContext context)
    {
        Context = context;
    }

    private JsonResult CreateResponse<T>(Option<T> dashboard) => dashboard
        .Match(
            Some: d => new JsonResult(new
            {
                success = true,
                dashboard = d
            }),
            None: () => new JsonResult(new
            {
                success = false
            }));

    [HttpGet]
    public Task<JsonResult> Index() =>
        Context.GetAsync<Db.Convex.Flyer>(Db.Convex.Flyer.ID).Map(CreateResponse);
}