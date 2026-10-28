namespace Bolt.Core.Model;

/// <summary>An API token for a forge host (rate limits, private repositories).</summary>
public class HostCredential
{
    public int Id { get; set; }

    public required string Host { get; set; }

    public required string Token { get; set; }
}
