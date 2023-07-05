using System.Text;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Llama.Airforce.Jobs.Functions;

public static class HttpFunctions
{
    /// <summary>
    /// Returns general json data from Snapshot
    /// </summary>
    public static Func<
            Func<HttpClient>,
            string,
            string,
            EitherAsync<Error, string>>
        PostData = fun((
            Func<HttpClient> httpFactory,
            string url,
            string bodyContent) =>
        TryAsync(async () =>
        {
            using var httpClient = httpFactory();
            var body = new StringContent(
                bodyContent,
                Encoding.UTF8,
                "application/json");

            var resp = await httpClient.PostAsync(url, body);
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Unable to get HTTP rest data for {url}, status code: {resp.StatusCode}");

            return await resp.Content.ReadAsStringAsync();
        })
        .ToEither());

    /// <summary>
    /// Returns general json data from Snapshot
    /// </summary>
    public static Func<
            Func<HttpClient>,
            string,
            EitherAsync<Error, string>>
        GetData = fun((
            Func<HttpClient> httpFactory,
            string url) =>
        TryAsync(async () =>
        {
            using var httpClient = httpFactory();

            var resp = await httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Unable to get HTTP rest data for {url}, status code: {resp.StatusCode}");

            return await resp.Content.ReadAsStringAsync();
        })
        .ToEither());
}
 
