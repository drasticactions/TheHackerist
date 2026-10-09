using System.Net;
using System.Text;

namespace TheHackerist.Tests.TestDoubles;

public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, string> _responses = [];

    public Dictionary<string, int> RequestCounts { get; } = [];

    public void SetResponse(string path, string json) => _responses[path] = json;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        RequestCounts[path] = RequestCounts.GetValueOrDefault(path) + 1;

        if (!_responses.TryGetValue(path, out var json))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
    }
}
