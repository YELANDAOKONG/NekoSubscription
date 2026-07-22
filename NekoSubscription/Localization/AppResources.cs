using System;
using System.Collections;
using System.Globalization;
using System.Resources;

namespace NekoSubscription.Localization;

internal static class AppResources
{
    public const string EnglishCultureName = "en";
    public const string SimplifiedChineseCultureName = "zh-Hans";
    public const string TraditionalChineseCultureName = "zh-Hant";

    private static readonly ResourceManager ResourceManager = new(
        "NekoSubscription.Resources.Strings",
        typeof(AppResources).Assembly);

    private static readonly string SystemCultureName = ResolveSupportedCulture(
        CultureInfo.CurrentUICulture);

    public static string CurrentCultureName { get; private set; } = SystemCultureName;

    public static string Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ??
            throw new MissingManifestResourceException($"The UI resource '{key}' is missing.");
    }

    public static string Format(string key, params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return string.Format(CultureInfo.CurrentCulture, Get(key), arguments);
    }

    public static void SetCulture(string? cultureName)
    {
        var supportedCultureName = ResolveSupportedCultureName(cultureName);
        var culture = CultureInfo.GetCultureInfo(supportedCultureName);

        CurrentCultureName = supportedCultureName;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    public static void ValidateResources()
    {
        var neutralResources = ResourceManager.GetResourceSet(
            CultureInfo.InvariantCulture,
            true,
            false) ?? throw new MissingManifestResourceException("The neutral UI resource set is missing.");

        ValidateCultureResources(neutralResources, SimplifiedChineseCultureName);
        ValidateCultureResources(neutralResources, TraditionalChineseCultureName);
    }

    private static string ResolveSupportedCultureName(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return SystemCultureName;
        }

        try
        {
            return ResolveSupportedCulture(CultureInfo.GetCultureInfo(cultureName));
        }
        catch (CultureNotFoundException)
        {
            return EnglishCultureName;
        }
    }

    private static string ResolveSupportedCulture(CultureInfo culture)
    {
        if (!culture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase))
        {
            return EnglishCultureName;
        }

        var normalizedName = culture.Name;
        var usesTraditionalChinese = normalizedName.Contains("Hant", StringComparison.OrdinalIgnoreCase) ||
            normalizedName.EndsWith("-TW", StringComparison.OrdinalIgnoreCase) ||
            normalizedName.EndsWith("-HK", StringComparison.OrdinalIgnoreCase) ||
            normalizedName.EndsWith("-MO", StringComparison.OrdinalIgnoreCase);
        return usesTraditionalChinese
            ? TraditionalChineseCultureName
            : SimplifiedChineseCultureName;
    }

    private static void ValidateCultureResources(ResourceSet neutralResources, string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var localizedResources = ResourceManager.GetResourceSet(culture, true, false) ??
            throw new MissingManifestResourceException(
                $"The UI resource set for culture '{cultureName}' is missing.");

        foreach (DictionaryEntry resource in neutralResources)
        {
            var key = (string)resource.Key;
            if (localizedResources.GetString(key) is null)
            {
                throw new MissingManifestResourceException(
                    $"The UI resource '{key}' is missing for culture '{cultureName}'.");
            }
        }
    }
}
