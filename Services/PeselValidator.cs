namespace Apteka.Services;

public static class PeselValidator
{
	public static bool IsValid(string? pesel)
	{
		if (string.IsNullOrWhiteSpace(pesel) || pesel.Length != 11 || !long.TryParse(pesel, out _))
			return false;
		int[] weights = [1, 3, 7, 9, 1, 3, 7, 9, 1, 3];
		var sum = 0;

		for (var i = 0; i < 10; i++)
			sum += (pesel[i] - '0') * weights[i];

		var controlDigit = pesel[10] - '0';
		var checksum = (10 - sum % 10) % 10;

		return controlDigit == checksum;
	}
}