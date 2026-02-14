namespace HttpsRichardy.Dispatcher.Extensions;

public static class DispatcherExtension
{
    public static void AddDispatcher(this IServiceCollection services, Action<DispatcherOptions> configure)
    {
        services.AddSingleton<IDispatcher, Dispatcher>();
        services.AddSingleton<IEventDispatcher, EventDispatcher>();

        var options = new DispatcherOptions();

        configure.Invoke(options);

        if (options.Assemblies.Count == 0)
            throw new InvalidOperationException("No assemblies were configured for dispatcher scanning.");

        var handlers = options.Assemblies.SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .SelectMany(type => type.GetInterfaces()
                .Where(metadata => metadata.IsGenericType)
                .Where(metadata =>
                    metadata.GetGenericTypeDefinition() == typeof(IDispatchHandler<,>) ||
                    metadata.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                .Select(metadata => new { Interface = metadata, Implementation = type })
            );

        foreach (var metadata in handlers)
        {
            services.AddTransient(metadata.Interface, metadata.Implementation);
        }
    }
}
