using Bolt.Core.Forges;

namespace Bolt.Core.Services;

/// <summary>Downloads release assets into a private temporary folder.</summary>
public sealed class AssetDownloader(AppPaths paths)
{
    /// <summary>Downloads the asset; the caller deletes the returned folder after use.</summary>
    /// <param name="progress">Receives 0–1, or nothing when the size is unknown.</param>
    public async Task<string> DownloadAsync(IForgeClient client, RepoRef repo, ReleaseAsset asset,
                                            IProgress<double>? progress, CancellationToken ct = default)
    {
        var folder = Path.Combine(paths.DownloadDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var fileName = Path.GetFileName(asset.Name.Replace('\\', '/'));
        var target = Path.Combine(folder, string.IsNullOrEmpty(fileName) ? "package" : fileName);

        try
        {
            using var response = await client.OpenDownloadAsync(repo, asset.DownloadUrl, ct).ConfigureAwait(false);
            var total = response.Content.Headers.ContentLength ?? asset.Size;
            await using var source = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var file = File.Create(target);

            var buffer = new byte[81920];
            long received = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                received += read;
                if (total > 0)
                    progress?.Report(Math.Min(1, (double)received / total.Value));
            }
            return target;
        }
        catch
        {
            TryDelete(folder);
            throw;
        }
    }

    public static void TryDelete(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
