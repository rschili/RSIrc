using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace RSIrc;

public sealed class IrcClient : IAsyncDisposable
{
    // Immutable dependencies
    private readonly TcpClient _tcpClient;
    private readonly Stream _stream;
    private readonly Channel<IrcEvent> _incoming;
    private readonly Channel<string> _outgoing;

    private readonly CancellationTokenSource _linkedCts;
    private readonly Task _readLoop;
    private readonly Task _writeLoop;
    private CancellationTokenSource? _shutdownCts;

    public ILogger Logger { get; private set; }

    public IrcClientOptions Options { get; private set; }

    public ChannelReader<IrcEvent> Events => _incoming.Reader;

    /// <summary>
    /// Send an IRC command to the server
    /// </summary>
    public async Task SendAsync(IrcCommand command, CancellationToken cancellationToken = default)
    {
        var rawCommand = command.ToIrcString();
        Logger.LogDebug("Sending: {Command}", rawCommand);
        await _outgoing.Writer.WriteAsync(rawCommand, cancellationToken);
    }

    /// <summary>
    /// Convenience method to send a message to a channel or user
    /// </summary>
    public Task SendMessageAsync(string target, string text, CancellationToken cancellationToken = default)
        => SendAsync(new IrcSendMessage(target, text), cancellationToken);

    /// <summary>
    /// Convenience method to join a channel
    /// </summary>
    public Task JoinChannelAsync(string channel, CancellationToken cancellationToken = default)
        => SendAsync(new IrcJoinChannel(channel), cancellationToken);

    private IrcClient(IrcClientOptions options, TcpClient tcpClient, Stream stream, CancellationToken cancellationToken, ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        _tcpClient = tcpClient;
        _stream = stream;
        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Channels can be created here, knowing the connection is alive
        _incoming = Channel.CreateUnbounded<IrcEvent>();
        _outgoing = Channel.CreateUnbounded<string>();

        // Start loops immediately
        _readLoop = Task.Run(() => ProcessReadsAsync(PipeReader.Create(_stream), _linkedCts.Token));
        _writeLoop = Task.Run(() => ProcessWritesAsync(_linkedCts.Token));
    }

    public static async Task<IrcClient> ConnectAsync(IrcClientOptions options, CancellationToken cancellationToken, ILogger? logger = null)
    {
        var loggerInstance = logger ?? NullLogger<IrcClient>.Instance;
        var tcpClient = new TcpClient();
        try
        {
            await tcpClient.ConnectAsync(options.Host, options.Port, cancellationToken).ConfigureAwait(false);

            Stream stream = tcpClient.GetStream();
            if (options.UseTls)
            {
                var ssl = new SslStream(stream, false, (s, c, h, e) => true);
                await ssl.AuthenticateAsClientAsync(options.Host).ConfigureAwait(false);
                stream = ssl;
            }

            await PerformLoginHandshake(stream, options).ConfigureAwait(false);

            return new IrcClient(options, tcpClient, stream, cancellationToken, loggerInstance);
        }
        catch
        {
            tcpClient.Dispose();
            throw;
        }
    }

    private static async Task PerformLoginHandshake(Stream stream, IrcClientOptions options)
    {
        // Simple write helper just for handshake
        byte[] bytes = Encoding.UTF8.GetBytes($"NICK {options.Nickname}\r\nUSER ...\r\n");
        await stream.WriteAsync(bytes);
        // In a real impl, you might wait for the '001' (Welcome) code here 
        // to ensure the connection is actually accepted before returning.
    }

    // --- LOOP 1: The Writer (Consumes Outgoing Channel) ---
    private async Task ProcessWritesAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var line in _outgoing.Reader.ReadAllAsync(ct))
            {
                var bytes = Encoding.UTF8.GetBytes(line + "\r\n");
                await _stream!.WriteAsync(bytes, ct);
                await _stream.FlushAsync(ct);
            }
        }
        catch (Exception)
        {
            // Handle disconnection / pipe broken
            _shutdownCts?.Cancel();
        }
    }

    // --- LOOP 2: The Reader (Feeds Incoming Channel) ---
    private async Task ProcessReadsAsync(PipeReader reader, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                ReadResult result = await reader.ReadAsync(ct);
                ReadOnlySequence<byte> buffer = result.Buffer;

                while (TryReadLine(ref buffer, out ReadOnlySequence<byte> lineBytes))
                {
                    // Convert to string only when we have a full line
                    var line = Encoding.UTF8.GetString(lineBytes);

                    // Priority Handling: PING
                    if (line.StartsWith("PING"))
                    {
                        var token = line.Substring(5);
                        await _outgoing.Writer.WriteAsync($"PONG {token}", ct);
                        continue; // Don't expose PING to user
                    }

                    // Parse & Push
                    var parsedEvent = IrcParser.Parse(line);
                    await _incoming.Writer.WriteAsync(parsedEvent, ct);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
                if (result.IsCompleted) break;
            }
        }
        catch (Exception) { _shutdownCts?.Cancel(); }
        finally { await _incoming.Writer.CompleteAsync(); }
    }

    // Efficient Pipeline Helper
    private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        // Look for \r\n or just \n
        SequencePosition? position = buffer.PositionOf((byte)'\n');
        if (position == null)
        {
            line = default;
            return false;
        }

        line = buffer.Slice(0, position.Value);

        // Update buffer to skip the line + the \n
        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        // 1. Stop the loops (if they haven't stopped already)
        await _linkedCts.CancelAsync();

        // 2. Wait for threads to finish (graceful shutdown)
        // We catch exceptions here because we EXPECT them to be cancelled.
        try { await Task.WhenAll(_readLoop, _writeLoop); }
        catch (OperationCanceledException) { }

        // 3. Free resources
        _stream.Dispose();
        _tcpClient.Dispose();
        _linkedCts.Dispose();
    }
}

