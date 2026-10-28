using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bolt.App.Resources;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bolt.App.Localization;

/// <param name="Code">Culture name of the satellite resources, e.g. "de".</param>
/// <param name="NativeName">The name of the language in itself, e.g. "Deutsch".</param>
public sealed record Language(string Code, string NativeName);

/// <summary>
/// The UI language. Texts bound with <see cref="TrExtension"/> follow it immediately; texts built in code are
/// refreshed through <see cref="LanguageChanged"/>.
/// </summary>
public sealed class Localizer : ObservableObject
{
    private const string LanguageKey = "language";

    private string? _settingsPath;

    private Localizer()
    {
    }

    /// <summary>The languages of the READMEs; the first one (English) is the default.</summary>
    public static IReadOnlyList<Language> Languages { get; } =
    [
        new("en", "English"),
        new("de", "Deutsch"),
        new("fr", "Français"),
        new("es", "Español"),
        new("pt", "Português"),
        new("zh", "中文"),
        new("ja", "日本語"),
        new("hu", "Magyar"),
    ];

    // After Languages: the constructor reads it.
    public static Localizer Instance { get; } = new();

    public Language Language { get; private set; } = Languages[0];

    public event EventHandler? LanguageChanged;

    public string this[string key] => Strings.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    /// <summary>Applies the language saved in <paramref name="settingsPath"/>; English when there is none.</summary>
    public void Load(string settingsPath)
    {
        _settingsPath = settingsPath;
        var code = ReadSavedCode(settingsPath);
        Apply(Languages.FirstOrDefault(l => l.Code == code) ?? Languages[0]);
    }

    /// <summary>Switches the UI language and saves the choice.</summary>
    public void SetLanguage(Language language)
    {
        if (language == Language)
            return;
        Apply(language);
        Save();
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Apply(Language language)
    {
        // The default thread culture covers the thread pool, where the core builds its messages.
        var culture = CultureInfo.GetCultureInfo(language.Code);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Language = language;
        OnPropertyChanged(nameof(Language));
    }

    private static string? ReadSavedCode(string path)
    {
        try
        {
            return File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path))?[LanguageKey]?.GetValue<string>() : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException
                                       or InvalidOperationException)
        {
            return null; // unreadable settings: start with the default language
        }
    }

    private void Save()
    {
        if (_settingsPath is null)
            return;
        try
        {
            File.WriteAllText(_settingsPath, new JsonObject { [LanguageKey] = Language.Code }.ToJsonString());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // the language still applies for this session
        }
    }
}
