namespace HttpsRichardy.Dispatcher.Contracts;

public interface IDispatcher
{
    public Task<TResult> DispatchAsync<TResult>(
        IDispatchable<TResult> dispatchable,
        CancellationToken cancellation = default
    );
}