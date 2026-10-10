using LanguageExt;
using Llama.Airforce.Database.Contexts;
using Llama.Airforce.Jobs.Factories;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;

namespace Llama.Airforce.Jobs.Jobs;

public class Dashboards
{
    public static Task UpdateDashboards(
        ILogger logger,
        IWeb3 web3,
        Func<HttpClient> httpFactory,
        DashboardContext context,
        DashboardFactory.Data data) =>
        DashboardFactory
            .CreateDashboards(web3, httpFactory, data)
            .MatchAsync(
                RightAsync: async dashboards =>
                {
                    foreach (var dashboard in dashboards)
                    {
                        await context.UpsertAsync(dashboard);
                        logger.LogInformation($"Updated dashboard: {dashboard.Id}");
                    }

                    return Unit.Default;
                },
                Left: ex =>
                {
                    logger.LogError($"Failed to update dashboard: {ex}");
                    return Unit.Default;
                });
}
