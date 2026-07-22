namespace NekoSubscription.ViewModels;

public sealed record SelectionOption<T>(string DisplayName, T Value)
{
    public override string ToString() => DisplayName;
}
