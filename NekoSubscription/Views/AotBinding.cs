using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace NekoSubscription.Views;

internal static class AotBinding
{
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "ViewModel properties are preserved via DynamicallyAccessedMembers on ViewModelBase.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "Avalonia fallback reflection binding is AOT-safe when ViewModel properties are preserved.")]
    public static Binding Path(string path, BindingMode mode = BindingMode.OneWay)
    {
        return new Binding(path) { Mode = mode };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "ViewModel properties are preserved via DynamicallyAccessedMembers on ViewModelBase.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "Avalonia fallback reflection binding is AOT-safe when ViewModel properties are preserved.")]
    public static Binding Path(string path, IValueConverter converter, BindingMode mode = BindingMode.OneWay)
    {
        return new Binding(path)
        {
            Converter = converter,
            Mode = mode
        };
    }
}
