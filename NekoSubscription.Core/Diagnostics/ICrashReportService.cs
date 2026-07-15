using System;

namespace NekoSubscription.Core.Diagnostics;

public interface ICrashReportService
{
    string? TryWriteReport(Exception exception, string source, bool isTerminating);
}
