using Avalonia;
using Avalonia.Controls;
using Material.Icons;

namespace Apteka.Views.Components;

public class IconButton : Button
{
	public static readonly StyledProperty<MaterialIconKind> IconProperty =
		AvaloniaProperty.Register<IconButton, MaterialIconKind>(nameof(Icon));

	public static readonly StyledProperty<string> TextProperty =
		AvaloniaProperty.Register<IconButton, string>(nameof(Text));

	public MaterialIconKind Icon
	{
		get => GetValue(IconProperty);
		set => SetValue(IconProperty, value);
	}

	public string Text
	{
		get => GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}
}