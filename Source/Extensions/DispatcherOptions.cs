namespace HttpsRichardy.Dispatcher.Extensions;

public sealed class DispatcherOptions
{
    internal readonly HashSet<Assembly> Assemblies = [];

    public DispatcherOptions ScanAssembly<TType>()
    {
        Assemblies.Add(typeof(TType).Assembly);
        return this;
    }
}
