namespace RSIrc;

/// <summary>
/// Base record for outgoing IRC commands
/// </summary>
public abstract record IrcCommand
{
    /// <summary>
    /// Converts the command to its raw IRC protocol string
    /// </summary>
    public abstract string ToIrcString();
}

public sealed record IrcSendMessage(string Target, string Text) : IrcCommand
{
    public override string ToIrcString() => $"PRIVMSG {Target} :{Text}";
}

public sealed record IrcJoinChannel(string Channel) : IrcCommand
{
    public override string ToIrcString() => $"JOIN {Channel}";
}

public sealed record IrcPartChannel(string Channel, string? Reason = null) : IrcCommand
{
    public override string ToIrcString() => 
        Reason != null ? $"PART {Channel} :{Reason}" : $"PART {Channel}";
}

public sealed record IrcSetMode(string Target, string Mode) : IrcCommand
{
    public override string ToIrcString() => $"MODE {Target} {Mode}";
}

public sealed record IrcQuit(string? Message = null) : IrcCommand
{
    public override string ToIrcString() => 
        Message != null ? $"QUIT :{Message}" : "QUIT";
}
