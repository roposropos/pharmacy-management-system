namespace Apteka.Services;

public static class PrivacyFormatter
{
	public static string MaskPesel(string? pesel)
	{
		if (string.IsNullOrWhiteSpace(pesel)) return string.Empty;
		if (pesel.Length <= 4) return new string('*', pesel.Length);
		return $"{new string('*', pesel.Length - 4)}{pesel[^4..]}";
	}
}
