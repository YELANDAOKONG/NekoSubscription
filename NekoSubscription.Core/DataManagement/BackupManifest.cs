using System;
using System.Text.Json.Serialization;

namespace NekoSubscription.Core.DataManagement;

public record BackupManifest(
    string Format,
    int Version,
    DateTimeOffset CreatedAtUtc,
    string[] Files);

[JsonSerializable(typeof(BackupManifest))]
public partial class BackupManifestJsonContext : JsonSerializerContext
{
}
