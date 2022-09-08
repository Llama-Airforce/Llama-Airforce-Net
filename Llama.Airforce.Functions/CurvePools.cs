using Llama.Airforce.Database.Contexts;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Functions;

public class CurvePools
{
    private readonly IConfiguration Config;
    private readonly CurvePoolContext CurvePoolContext;
    private readonly CurvePoolSnapshotsContext CurvePoolSnapshotsContext;
    private readonly CurvePoolRatiosContext CurvePoolRatiosContext;

    public CurvePools(
        IConfiguration config,
        CurvePoolContext curvePoolContext,
        CurvePoolSnapshotsContext curvePoolSnapshotsContext,
        CurvePoolRatiosContext curvePoolRatiosContext)
    {
        Config = config;
        CurvePoolContext = curvePoolContext;
        CurvePoolSnapshotsContext = curvePoolSnapshotsContext;
        CurvePoolRatiosContext = curvePoolRatiosContext;
    }

    [FunctionName("CurvePools")]
    public async Task Run(
        [TimerTrigger("0 0 */12 * * *", RunOnStartup = false)] TimerInfo curvePoolsTimer,
        ILogger log)
    {
        var poolsCurve = await Jobs.Jobs.CurvePools.UpdateCurvePools(log, CurvePoolContext);

        var alchemyEndpoint = Config.GetValue<string>("ALCHEMY");
        var snapshots = List<Database.Models.Curve.CurvePoolSnapshots>();

        // This is a foreach because SequenceSeries does not work for some reason.
        foreach (var pool in poolsCurve)
        {
            var snapshot = await Jobs.Jobs.CurvePools.UpdateCurvePoolSnapshots(
                log,
                alchemyEndpoint,
                CurvePoolSnapshotsContext,
                pool);

            snapshot.IfSome(s => snapshots = snapshots.Add(s));
        }

        foreach (var snapshot in snapshots)
            await Jobs.Jobs.CurvePools.UpdateCurvePoolRatios(log, CurvePoolRatiosContext, snapshot);
    }
}