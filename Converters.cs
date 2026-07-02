using System;
using System.Globalization;
using Apteka.Models;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Material.Icons;

namespace Apteka;

public class RowConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is true) return new SolidColorBrush(new Color(50, 255, 165, 0));
		return Brushes.Transparent;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}

public class TypConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value switch
		{
			Typ.Lek => MaterialIconKind.Drugs,
			Typ.Surowiec => MaterialIconKind.Material,
			_ => null
		};
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}