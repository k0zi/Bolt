using System.Net;
using System.Text.Json;
using Bolt.Core.Model;
using Bolt.Core.Resources;

namespace Bolt.Core.Forges;

/// <summary>Supplies the API token configured for a host, if any.</summary>
public interface ITokenProvider
{
    Task<string?> GetTokenAsync(string host, CancellationToken ct = default);
}

public abstract class ForgeClientBase(HttpClient http, ITokenProvider tokens) : IForgeClient
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public abstract ForgeKind Kind { get; }

    public abstract Task<IReadOnlyList<Release>> GetReleasesAsync(RepoRef repo, CancellationToken ct = default);

    public abstract Task<byte[]?> TryGetFileAsync(RepoRef repo, string path, CancellationToken ct = default);

    /// <summary>Adds the authentication header for <paramref name="token"/>.</summary>
    protected abstract void Authorize(HttpRequestMessage request, string token);

    /// <summary>
    /// Starts downloading a release asset; the token is sent only when the asset is served by the repository's host.
    /// </summary>
    public async Task<HttpResponseMessage> OpenDownloadAsync(RepoRef repo, string url, CancellationToken ct = default)
    {
        var sameHost = Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                       string.Equals(uri.Authority, repo.Host, StringComparison.OrdinalIgnoreCase);
        var response = await SendAsync(repo, url, "application/octet-stream", ct, authorize: sameHost,
                                       HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            using (response)
                EnsureSuccess(repo, response);
        }
        return response;
    }

    protected async Task<HttpResponseMessage> SendAsync(RepoRef repo, string url, string? accept,
                                                        CancellationToken ct, bool authorize = true,
                                                        HttpCompletionOption completion =
                                                            HttpCompletionOption.ResponseContentRead)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (accept is not null)
            request.Headers.Accept.ParseAdd(accept);
        if (authorize)
        {
            var token = await tokens.GetTokenAsync(repo.Host, ct).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(token))
                Authorize(request, token);
        }

        try
        {
            return await http.SendAsync(request, completion, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new ForgeException(string.Format(Strings.HostUnreachable, repo.Host, ex.Message), ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ForgeException(string.Format(Strings.HostTimeout, repo.Host), ex);
        }
    }

    protected async Task<string> GetJsonAsync(RepoRef repo, string url, CancellationToken ct)
    {
        using var response = await SendAsync(repo, url, "application/json", ct).ConfigureAwait(false);
        EnsureSuccess(repo, response);
        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }

    internal static T Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? throw new ForgeException(Strings.EmptyResponse);
        }
        catch (JsonException ex)
        {
            throw new ForgeException(string.Format(Strings.InvalidResponse, ex.Message), ex);
        }
    }

    protected async Task<byte[]?> GetBytesOrNullAsync(RepoRef repo, string url, string? accept, CancellationToken ct)
    {
        using var response = await SendAsync(repo, url, accept, ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        EnsureSuccess(repo, response);
        return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
    }

    private static void EnsureSuccess(RepoRef repo, HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var message = response.StatusCode switch
        {
            HttpStatusCode.NotFound => string.Format(Strings.RepoNotFound, repo.Url),
            HttpStatusCode.Unauthorized => string.Format(Strings.AuthRequired, repo.Host),
            HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests =>
                string.Format(Strings.RateLimited, repo.Host),
            _ => string.Format(Strings.HostError, repo.Host, (int)response.StatusCode, response.ReasonPhrase),
        };
        throw new ForgeException(message);
    }
}
