using System.Net.Http.Headers;

namespace PlanWise.Research.LlmEvaluation;

/// <summary>
/// Captures the raw request and response bodies of every call the production model makes.
///
/// This is the methodological core of the harness. Both Anthropic clients repair the model's answer
/// on the way out — the prioritiser silently drops unknown or duplicated task keys and appends
/// anything the model forgot, and the cost client re-labels nothing but never re-checks the sums.
/// Measuring only the returned object would therefore describe the *system's* behaviour, not the
/// *model's*, and the research question asks about the model. Recording at the transport layer gives
/// both: the raw tool call as emitted, and the object the application finally hands back.
///
/// It also keeps the experiment honest about cost-estimate caching: the harness calls the model
/// directly rather than through the job handler, so the input-hash cache that would return a stored
/// answer for identical input never runs.
/// </summary>
internal sealed class RecordingHandler : DelegatingHandler
{
    private readonly List<Exchange> exchanges = [];

    public RecordingHandler()
        : base(new HttpClientHandler())
    {
    }

    public IReadOnlyList<Exchange> Exchanges => exchanges;

    public void Clear() => exchanges.Clear();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        string requestBody = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

        DateTimeOffset started = DateTimeOffset.UtcNow;
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
        TimeSpan elapsed = DateTimeOffset.UtcNow - started;

        // Buffer the body so the caller can still read it after we have.
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var buffered = new StringContent(responseBody);
        foreach (var header in response.Content.Headers)
        {
            buffered.Headers.Remove(header.Key);
            buffered.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        response.Content = buffered;

        exchanges.Add(new Exchange(
            requestBody,
            responseBody,
            (int)response.StatusCode,
            elapsed.TotalSeconds));

        return response;
    }

    internal sealed record Exchange(string RequestBody, string ResponseBody, int StatusCode, double ElapsedSeconds);
}
