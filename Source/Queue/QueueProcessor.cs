namespace Dispattchr.Queue;

internal sealed class QueueProcessor<TContext>(MemoryQueue<TContext> queue, IServiceScopeFactory factory, ILogger<QueueProcessor<TContext>> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var context in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = factory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<IQueueHandler<TContext>>();
 
                await handler.HandleAsync(context, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "an unhandled exception occurred while processing a queued item of type {ContextType}.", typeof(TContext).Name);
            }
        }
    }
}
