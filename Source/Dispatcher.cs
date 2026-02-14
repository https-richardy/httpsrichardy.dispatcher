namespace HttpsRichardy.Dispatcher;

internal sealed class Dispatcher(IServiceProvider services) : IDispatcher
{
    public async Task<TResult> DispatchAsync<TResult>(
        IDispatchable<TResult> dispatchable, CancellationToken cancellation = default)
    {
        var dispatchableType = dispatchable.GetType();
        var handlerInterfaceType = typeof(IDispatchHandler<,>).MakeGenericType(dispatchableType, typeof(TResult));

        var handler = services.GetService(handlerInterfaceType);
        if (handler is null)
        {
            throw new InvalidOperationException($"No handler registered for dispatchable type {dispatchableType.FullName}");
        }

        var handleMethod = handlerInterfaceType.GetMethod("HandleAsync", BindingFlags.Public | BindingFlags.Instance)!;
        var task = (Task<TResult>) handleMethod.Invoke(handler, [dispatchable, cancellation])!;

        return await task;
    }
}
