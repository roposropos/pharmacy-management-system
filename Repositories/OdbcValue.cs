using System;
using System.Globalization;

namespace Apteka.Repositories;

internal static class OdbcValue
{
	public static bool ToBoolean(object value)
	{
		if (value == DBNull.Value) return false;

		return value switch
		{
			bool boolean => boolean,
			byte number => number != 0,
			short number => number != 0,
			int number => number != 0,
			long number => number != 0,
			decimal number => number != 0,
			double number => Math.Abs(number) > double.Epsilon,
			float number => Math.Abs(number) > float.Epsilon,
			string text => ToBoolean(text),
			_ => Convert.ToBoolean(value, CultureInfo.InvariantCulture)
		};
	}

	private static bool ToBoolean(string value)
	{
		var normalized = value.Trim();
		if (bool.TryParse(normalized, out var boolean)) return boolean;
		if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
			return number != 0;

		return normalized.Equals("t", StringComparison.OrdinalIgnoreCase)
		       || normalized.Equals("y", StringComparison.OrdinalIgnoreCase)
		       || normalized.Equals("yes", StringComparison.OrdinalIgnoreCase);
	}
}
