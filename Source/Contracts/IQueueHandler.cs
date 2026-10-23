namespace HttpsRichardy.Dispatcher.Contracts;

public interface IQueueHandler<TContext>
{
    public Task HandleAsync(
        TContext context,
        CancellationToken cancellation = default
    );
}
