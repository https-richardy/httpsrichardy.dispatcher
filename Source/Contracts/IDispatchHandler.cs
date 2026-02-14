namespace HttpsRichardy.Dispatcher.Contracts;

public interface IDispatchHandler<TDispatchable, TResult>
    where TDispatchable : IDispatchable<TResult>
{
    public Task<TResult> HandleAsync(
        TDispatchable parameters,
        CancellationToken cancellation = default
    );
}