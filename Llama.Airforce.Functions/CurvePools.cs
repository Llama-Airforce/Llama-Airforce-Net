using Llama.Airforce.Database.Contexts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Functions;

public class CurvePools
{
    private readonly ILogger Logger;
    private readonly IConfiguration Config;
    private readonly CurvePoolContext CurvePoolContext;
    private readonly CurvePoolSnapshotsContext CurvePoolSnapshotsContext;
    private readonly CurvePoolRatiosContext CurvePoolRatiosContext;
    private readonly IHttpClientFactory HttpClientFactory;

    public CurvePools(
        ILoggerFactory loggerFactory,
        IConfiguration config,
        CurvePoolContext curvePoolContext,
        CurvePoolSnapshotsContext curvePoolSnapshotsContext,
        CurvePoolRatiosContext curvePoolRatiosContext,
        IHttpClientFactory httpClientFactory)
    {
        Logger = loggerFactory.CreateLogger<CurvePools>();
        Config = config;
        CurvePoolContext = curvePoolContext;
        CurvePoolSnapshotsContext = curvePoolSnapshotsContext;
        CurvePoolRatiosContext = curvePoolRatiosContext;
        HttpClientFactory = httpClientFactory;
    }

    [Function("CurvePools")]
    public async Task Run(
        [TimerTrigger("0 0 0 * * *", RunOnStartup = false)] TimerInfo curvePoolsTimer)
    {
        var poolsCurve = await Jobs.Jobs.CurvePools.UpdateCurvePools(
            Logger,
            HttpClientFactory.CreateClient,
            CurvePoolContext);

        var alchemyEndpoint = Config.GetValue<string>("ALCHEMY");
        var snapshots = List<Database.Models.Curve.CurvePoolSnapshots>();

        // This is a foreach because SequenceSeries does not work for some reason.
        foreach (var pool in poolsCurve)
        {
            var snapshot = await Jobs.Jobs.CurvePools.UpdateCurvePoolSnapshots(
                Logger,
                HttpClientFactory.CreateClient,
                alchemyEndpoint,
                CurvePoolSnapshotsContext,
                pool);

            snapshot.IfSome(s => snapshots = snapshots.Add(s));
        }

        foreach (var snapshot in snapshots)
            await Jobs.Jobs.CurvePools.UpdateCurvePoolRatios(Logger, CurvePoolRatiosContext, snapshot);
    }
}