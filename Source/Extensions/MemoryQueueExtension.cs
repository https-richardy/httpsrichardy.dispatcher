namespace Dispattchr.Extensions;

public static class MemoryQueueExtension
{
    public static void AddMemoryQueue(this IServiceCollection services, Action<DispatcherOptions> configure)
    {
        var options = new DispatcherOptions();

        configure.Invoke(options);

        var handlers = options.Assemblies.SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .SelectMany(type => type.GetInterfaces()
                .Where(metadata => metadata.IsGenericType)
                .Where(metadata => metadata.GetGenericTypeDefinition() == typeof(IQueueHandler<>))
                .Select(metadata => new { Context = metadata.GetGenericArguments()[0], Interface = metadata, Implementation = type })
            );

        foreach (var group in handlers.GroupBy(metadata => metadata.Context))
        {
            var context = group.Key;
            if (group.Count() > 1)
            {
                var competitors = string.Join(", ", group.Select(metadata => metadata.Implementation.FullName));

                throw new InvalidOperationException(
                    $"Multiple queue handlers were found for context type {context.FullName}: {competitors}. " +
                    "A memory queue context must have exactly one handler."
                );
            }

            var metadata = group.Single();

            var queueType = typeof(MemoryQueue<>).MakeGenericType(context);
            var enqueuerType = typeof(IEnqueuer<>).MakeGenericType(context);
            var processorType = typeof(QueueProcessor<>).MakeGenericType(context);

            services.AddSingleton(queueType);
            services.AddSingleton(enqueuerType, provider => provider.GetRequiredService(queueType));
            services.AddScoped(metadata.Interface, metadata.Implementation);
            services.AddSingleton(typeof(IHostedService), processorType);
        }
    }
}
