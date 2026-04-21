using System;

namespace SilvaData.Infrastructure;

public static class ServiceHelper
{
    public static IServiceProvider Services { get; private set; } = default!;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        Services = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public static T GetRequiredService<T>() where T : notnull
    {
        if (Services == null)
            throw new InvalidOperationException("Service provider not initialized. Call ServiceHelper.Initialize in MauiProgram.");

        return Services.GetService(typeof(T)) is T instance
            ? instance
            : throw new InvalidOperationException($"Service of type {typeof(T).FullName} is not registered.");
    }
}