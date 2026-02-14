namespace HttpsRichardy.Dispatcher;

internal sealed class EventDispatcher(IServiceProvider services) : IEventDispatcher
{
    public async Task DispatchAsync<TEvent>(TEvent dispatchable, CancellationToken cancellation = default)
        where TEvent : IEvent
    {
        var handlers = services.GetServices<IEventHandler<TEvent>>();

        await Parallel.ForEachAsync(handlers, cancellation, async (handler, cancellationToken) =>
        {
            await handler.HandleAsync(dispatchable, cancellationToken);
        });
    }
}
