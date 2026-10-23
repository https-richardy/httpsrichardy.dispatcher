namespace Dispattchr.Queue;

internal sealed class MemoryQueue<TContext> : IEnqueuer<TContext>
{
    private readonly Channel<TContext> _channel = Channel.CreateUnbounded<TContext>(new UnboundedChannelOptions
    {
        SingleReader = true
    });

    public ChannelReader<TContext> Reader => _channel.Reader;
    public ChannelWriter<TContext> Writer => _channel.Writer;

    public ValueTask EnqueueAsync(TContext context, CancellationToken cancellation = default)
        => _channel.Writer.WriteAsync(context, cancellation);
}
