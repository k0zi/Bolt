using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Bolt.App.Localization;

/// <summary><c>{l:Tr Key}</c>: a text of <c>Resources/Strings.resx</c> in the current UI language.</summary>
public sealed class TrExtension(string key) : MarkupExtension
{
    public string Key { get; } = key;

    // Bound to the language, so the text is looked up again when it changes.
    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new ReflectionBindingExtension(nameof(Localizer.Language))
        {
            Source = Localizer.Instance,
            Mode = BindingMode.OneWay,
            Converter = new FuncValueConverter<Language, string>(_ => Localizer.Instance[Key]),
        }.ProvideValue(serviceProvider);
}
