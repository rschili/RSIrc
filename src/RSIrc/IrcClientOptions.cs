namespace RSIrc;

public record IrcClientOptions
{
    // Required: Connection basics
    public required string Host { get; init; }
    public required string Nickname { get; init; }

    // Optional: Defaults to standard TLS port (6697)
    public int Port { get; init; } = 6697;

    // Optional: Defaults to true for security
    public bool UseTls { get; init; } = true;

    // Optional: Metadata
    // If null, library should default these to the Nickname
    public string? Username { get; init; } 
    public string? RealName { get; init; }

    // Optional: Server password (or SASL password in complex setups)
    public string? Password { get; init; }

    // Optional: List of channels to join automatically upon connect
    public IEnumerable<string> AutoJoinChannels { get; init; } = Array.Empty<string>();

    // Dev Helper: Allow self-signed certs (set to true for localhost testing)
    public bool AcceptInvalidCertificates { get; init; } = false;
}