namespace HttpsRichardy.Dispatcher.Contracts;

public interface IEventHandler<TEvent>
    where TEvent : IEvent
{
    public Task HandleAsync(
        TEvent parameters,
        CancellationToken cancellation = default
    );
}
