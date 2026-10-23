namespace Dispattchr.Contracts;

public interface IEventDispatcher
{
    public Task DispatchAsync<TEvent>(TEvent dispatchable, CancellationToken cancellation = default)
        where TEvent : IEvent;
}