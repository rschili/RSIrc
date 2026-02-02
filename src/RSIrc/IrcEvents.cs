namespace RSIrc;

public abstract record IrcEvent;

public sealed record IrcMessage(string User, string Channel, string Text) : IrcEvent;
public sealed record IrcPrivateMessage(string Author, string Target, string Text) : IrcEvent;
public sealed record IrcJoin(string User, string Channel) : IrcEvent;
public sealed record IrcPart(string User, string Channel) : IrcEvent;
// ... etc