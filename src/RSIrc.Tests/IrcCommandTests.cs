using RSIrc;

namespace RSIrc.Tests;

public class IrcCommandTests
{
    [Test]
    [Arguments("#channel", "Hello world", "PRIVMSG #channel :Hello world")]
    [Arguments("nick", "private message", "PRIVMSG nick :private message")]
    [Arguments("#test", "Message with :colons", "PRIVMSG #test :Message with :colons")]
    [Arguments("#lobby", "", "PRIVMSG #lobby :")]
    public async Task IrcSendMessage_FormatsCorrectly(string target, string text, string expected)
    {
        var command = new IrcSendMessage(target, text);
        var result = command.ToIrcString();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments("#channel", "JOIN #channel")]
    [Arguments("#test", "JOIN #test")]
    [Arguments("#lobby", "JOIN #lobby")]
    [Arguments("&local", "JOIN &local")]
    public async Task IrcJoinChannel_FormatsCorrectly(string channel, string expected)
    {
        var command = new IrcJoinChannel(channel);
        var result = command.ToIrcString();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments("#channel", null, "PART #channel")]
    [Arguments("#test", "Leaving now", "PART #test :Leaving now")]
    [Arguments("#lobby", "Goodbye!", "PART #lobby :Goodbye!")]
    [Arguments("#dev", "", "PART #dev :")]
    public async Task IrcPartChannel_FormatsCorrectly(string channel, string? reason, string expected)
    {
        var command = new IrcPartChannel(channel, reason);
        var result = command.ToIrcString();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments("#channel", "+m", "MODE #channel +m")]
    [Arguments("#test", "-i", "MODE #test -i")]
    [Arguments("nickname", "+o", "MODE nickname +o")]
    [Arguments("#lobby", "+v user", "MODE #lobby +v user")]
    public async Task IrcSetMode_FormatsCorrectly(string target, string mode, string expected)
    {
        var command = new IrcSetMode(target, mode);
        var result = command.ToIrcString();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments(null, "QUIT")]
    [Arguments("Goodbye!", "QUIT :Goodbye!")]
    [Arguments("Connection lost", "QUIT :Connection lost")]
    [Arguments("", "QUIT :")]
    public async Task IrcQuit_FormatsCorrectly(string? message, string expected)
    {
        var command = new IrcQuit(message);
        var result = command.ToIrcString();
        await Assert.That(result).IsEqualTo(expected);
    }
}