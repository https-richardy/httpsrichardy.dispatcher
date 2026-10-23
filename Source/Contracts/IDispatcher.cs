namespace Dispattchr.Contracts;

public interface IDispatcher
{
    public Task<TResult> DispatchAsync<TResult>(
        IDispatchable<TResult> dispatchable,
        CancellationToken cancellation = default
    );
}