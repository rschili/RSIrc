using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace RSIrc.Tests;
public class IrcClientTests
{
    private readonly ILogger _logger = NullLogger<IrcClientTests>.Instance;

    public IrcClientTests()
    {
    }

    [Test, Explicit]
    public async Task ConnectAsync_LocalErgo()
    {
        var options = new IrcClientOptions
        {
            Host = "localhost",
            Nickname = "TestUser",
            AcceptInvalidCertificates = true
        };
        await using var client = await IrcClient.ConnectAsync(options, CancellationToken.None, _logger);
        await Assert.That(client).IsNotNull();
    }
}

