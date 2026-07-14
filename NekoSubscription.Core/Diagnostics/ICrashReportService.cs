using System;

namespace NekoSubscription.Core.Diagnostics;

public interface ICrashReportService
{
    string? Report(Exception exception, CrashSource source);
}
