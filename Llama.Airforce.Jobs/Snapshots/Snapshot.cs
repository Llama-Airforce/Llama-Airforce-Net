using LanguageExt;
using LanguageExt.Common;
using Llama.Airforce.Jobs.Extensions;
using Llama.Airforce.Jobs.Snapshots.Models;
using Llama.Airforce.SeedWork.Extensions;
using Newtonsoft.Json;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Snapshots;

public static class Snapshot
{
    public const string SNAPSHOT_URL = "https://hub.snapshot.org/graphql";

    /// <summary>
    /// Returns general json data from Snapshot
    /// </summary>
    public static Func<
            Func<HttpClient>,
            string,
            string,
            EitherAsync<Error, string>>
        GetData = fun((
            Func<HttpClient> httpFactory,
            string url,
            string query) => Functions
        .HttpFunctions
        .PostData(
            httpFactory,
            url,
            JsonConvert.SerializeObject(new
            {
                query
            })));

    /// <summary>
    /// Returns a mapping of proposals data from The Graph for a specific space.
    /// The key is the original proposal id, the value is the hash used by the bribes platform.
    /// </summary>
    public static Func<
            Func<HttpClient>,
            string,
            Func<Proposal, bool>,
            Func<string, string>,
            EitherAsync<Error, Map<string, (int Index, string Id)>>>
        GetProposalIds = fun((
            Func<HttpClient> httpFactory,
            string space,
            Func<Proposal, bool> filter,
            Func<string, string> valueMap) =>
        {
            string Query = $@"{{
proposals(
    where: {{ space: ""{space}"" }}
    first: 1000
    orderBy: ""created""
    orderDirection: asc
) {{
    id
    title
}} }}";

            return GetData(httpFactory, SNAPSHOT_URL, Query)
                .MapTry(JsonConvert.DeserializeObject<RequestProposals>)
                .MapTry(data => toMap(data
                    .Data
                    .ProposalList
                    .Select((p, i) => (Proposal: p, Index: i))
                    .Where(x => filter(x.Proposal))
                    .Select(x => (
                        Key: x.Proposal.Id,
                        Value: (x.Index, Id: valueMap(x.Proposal.Id))))));
        });

    /// <summary>
    /// Returns general proposal data from The Graph.
    /// </summary>
    public static Func<
            Func<HttpClient>,
            string,
            EitherAsync<Error, Proposal>>
        GetProposal = fun((
            Func<HttpClient> httpFactory,
            string id) =>
        {
            var query = $@"{{
proposals(
    where: {{ id: ""{id}"" }},
) {{
    id
    choices
    start
    end
    snapshot
    scores_total
}} }}";

            return GetData(httpFactory, SNAPSHOT_URL, query)
                .MapTry(JsonConvert.DeserializeObject<RequestProposals>)
                .MapTry(
                    data => data.Data.ProposalList.Single(),
                    ex => $"Failed to get Snapshot proposal {id}");
        });

    /// <summary>
    /// Returns how many choices a proposal has.
    /// </summary>
    public static Func<
            Func<HttpClient>,
            string,
            EitherAsync<Error, int>>
        GetNumChoices = fun((
            Func<HttpClient> httpFactory,
            string id) =>
        {
            var query = $@"{{
proposals(
    where: {{ id: ""{id}"" }},
) {{
    choices
}} }}";

            return GetData(httpFactory, SNAPSHOT_URL, query)
                .MapTry(JsonConvert.DeserializeObject<RequestProposals>)
                .MapTry(
                    data => data.Data.ProposalList.Single().Choices.Count,
                    ex => $"Failed to get Snapshot proposal {id}");
        });

    /// <summary>
    /// Returns vote data for a specific proposal.
    /// </summary>
    public static Func<
            Func<HttpClient>,
            string,
            EitherAsync<Error, Lst<Vote>>>
        GetVotes = fun((
            Func<HttpClient> httpFactory,
            string id) =>
        {
            var fs = fun((int page, int offset) =>
            {
                var query = $@"{{
votes(
    where: {{ proposal: ""{id}"" }},
    first: {offset}
    skip: {page * offset}
    orderBy: ""created""
    orderDirection: desc
) {{
    id
    voter
    choice
}} }}";

                return GetData(httpFactory, SNAPSHOT_URL, query)
                    .MapTry(JsonConvert.DeserializeObject<RequestVotes>)
                    .MapTry(x => toList(x.Data.VoteList));
            });

            return fs.Paginate().Map(votes =>
            {
                // Fix for HiddenHand fucking up their vote in Aura round 21.
                // It was added later manually, hence we hardcode this vote in.
                // https://snapshot.org/#/aurafinance.eth/proposal/0x051e501623ca3f97d1131b04ae55201ed0f4226c523ac9acebc96a7c4c5c823d
                if (id == "0xdf8a01486bd5e3a8c11b161f2a43f7295b0cd4ff257d5de95e485512287157b3")
                    votes += VOTE_HH;

                return votes;
            });
        });

    public static Vote VOTE_HH = new()
    {
        Voter = "0x3CdE8FA1c73AfE0828f672D197e082463C2aC8e2",
        Choices = new Dictionary<string, double>
        {
            { "34", 1003664 },
            { "10", 797362 },
            { "3", 735059 },
            { "50", 595202 },
            { "41", 505444 },
            { "30", 178861 },
            { "46", 153029 },
            { "39", 132059 },
            { "63", 130284 },
            { "57", 115001 },
            { "12", 114928 },
            { "42", 97179 },
            { "2", 87768 },
            { "52", 78816 },
            { "62", 72466 },
            { "51", 66378 },
            { "53", 50156 },
            { "72", 41716 },
            { "58", 37209 },
            { "49", 33585 },
            { "75", 29596 },
            { "74", 29376 },
            { "25", 26741 },
            { "69", 24724 },
            { "29", 24521 },
            { "73", 23503 },
            { "77", 20734 },
            { "78", 13528 },
            { "66", 10177 },
            { "54", 9735 },
            { "86", 8117 },
            { "56", 689 },
        }
    };
}