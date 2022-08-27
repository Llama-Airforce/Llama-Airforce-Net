using LanguageExt;
using Llama.Airforce.Database.Contexts;
using Microsoft.AspNetCore.Mvc;
using Db = Llama.Airforce.Database.Models;
using Overview = Llama.Airforce.Database.Models.Bribes.Dashboards.Overview;

namespace Llama.Airforce.API.Controllers;

[ApiController]
[Route("[controller]")]
public class DashboardController : ControllerBase
{
    private readonly DashboardContext Context;

    public DashboardController(
        DashboardContext context)
    {
        Context = context;
    }

    public class IndexParams
    {
        public string Id { get; set; }
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

    [HttpPost]
    public Task<JsonResult> Index([FromBody] IndexParams body)
    {
        return body.Id switch
        {
            Overview.Votium => Context.GetAsync<Overview>(body.Id).Map(CreateResponse),
            Overview.Aura => Context.GetAsync<Overview>(body.Id).Map(CreateResponse),
            Db.Convex.Flyer.ID => Context.GetAsync<Db.Convex.Flyer>(body.Id).Map(CreateResponse),
            Db.Aura.Flyer.ID => Context.GetAsync<Db.Aura.Flyer>(body.Id).Map(CreateResponse),
            _ => Context.GetAsync<Database.Dashboard>(body.Id).Map(CreateResponse)
        };
    }
}