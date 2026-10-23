namespace HttpsRichardy.Dispatcher.Contracts;

public interface IEnqueuer<TContext>
{
    public ValueTask EnqueueAsync(
        TContext context,
        CancellationToken cancellation = default
    );
}
