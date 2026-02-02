using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RSIrc;

var options = new IrcClientOptions
{
    Host = "localhost",
    Nickname = "Luebke",
    // Port defaults to 6697
    // UseTls defaults to true
    AcceptInvalidCertificates = true // For local Ergo container
};


//set up dependency injection. Only used for ILogger, completely optional, you do not have to pass a logger into ConnectAsync
var services = new ServiceCollection()
    .AddLogging(logging =>
    {
        logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "hh:mm:ss ";
        });
        logging.SetMinimumLevel(LogLevel.Debug);
    })
    .BuildServiceProvider();

//Using CancellationToken as a shutdown mechanism
var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{ // allows shutting down the app using Ctrl+C
    e.Cancel = true;
    cancellationTokenSource.Cancel();
};

try
{
    // Returns a connected, authenticated client (handles CAP negotiation internally)
    await using var client = await IrcClient.ConnectAsync(options,
        cancellationTokenSource.Token, services.GetRequiredService<ILogger<IrcClient>>());

    // 3. IRC Specific: You must explicitly join channels after connecting
    await client.JoinChannelAsync("#dev");

    await foreach (var message in client.Events.ReadAllAsync(cancellationTokenSource.Token))
    {
        await OnMessageAsync(client, message);
    }
    Console.WriteLine("sync has ended.");
}
catch (OperationCanceledException)
{
    Console.WriteLine("clean shutdown.");
}
catch (Exception ex)
{
    Console.WriteLine("An error occurred (app shut down):");
    Console.WriteLine(ex.Message);
}
finally
{
    await services.DisposeAsync();
}


async Task OnMessageAsync(IrcClient client, IrcEvent message)
{
    // IRC has many event types (Join, Part, Mode). We filter for channel messages.
    if (message is IrcMessage chat) 
    {
        Console.WriteLine($"[{chat.Channel}] {chat.User}: {chat.Text}");

        if (chat.Text.Contains("ping"))
        {
            // Send a reply to the same channel
            await client.SendMessageAsync(chat.Channel, "pong!");
        }
    }
}