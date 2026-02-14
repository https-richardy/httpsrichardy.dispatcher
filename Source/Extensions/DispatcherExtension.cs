namespace HttpsRichardy.Dispatcher.Extensions;

public static class DispatcherExtension
{
    public static void AddDispatcher(this IServiceCollection services, params Type[] markerTypes)
    {
        services.AddSingleton<IDispatcher, Dispatcher>();
        services.AddSingleton<IEventDispatcher, EventDispatcher>();

        var assemblies = markerTypes.Select(type => type.Assembly)
            .Distinct()
            .ToArray();

        var handlers = assemblies
            .SelectMany(assembly => assembly.GetTypes())
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