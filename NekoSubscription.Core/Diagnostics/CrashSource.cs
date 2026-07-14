namespace NekoSubscription.Core.Diagnostics;

public enum CrashSource
{
    ApplicationDomain,
    AvaloniaDispatcher,
    UnobservedTask,
    Startup
}
