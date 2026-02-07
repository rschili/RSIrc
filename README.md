# RSIrc

## Scope

Needed a library for an IRC bot, but only found outdated ones, so I decided to write one myself.
Spec: https://ircv3.net/

** WIP at the moment this is merely a draft, it is not implemented yet. **

## Example

Check the github repository for a minimalistic [console example](https://github.com/rschili/RSIrc/blob/main/src/RSIrc.Console/Program.cs).
Here is the basic usage:

```cs
MatrixTextClient client = await MatrixTextClient.ConnectAsync(userid, password, device,
    httpClientFactory, cancellationToken, logger);
```

and to handle messages:

```cs
await foreach (var message in client.Messages.ReadAllAsync(cancellationToken))
{
    Console.WriteLine(message);
    await message.Room.SendTypingNotificationAsync();
    if(message.Body.Equals("ping", StringComparison.OrdinalIgnoreCase))
        await message.SendResponseAsync("pong!");
}
```

Room and user information is cached and updated on the client object. Associated rooms and users are included in the message given to the handler.
The Messages channel is closed when the client is disconnected through CancellationToken or if a request fails. Consider reconnecting in that case by calling ConnectAsync() again.
