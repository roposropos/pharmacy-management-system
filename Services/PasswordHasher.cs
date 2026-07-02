using System;
using System.Security.Cryptography;
using System.Text;

namespace Apteka.Services;

public static class PasswordHasher
{
	private const int Iterations = 100_000;
	private const int SaltSize = 16;
	private const int HashSize = 32;
	private const string Prefix = "pbkdf2";

	public static string Hash(string password)
	{
		if (string.IsNullOrWhiteSpace(password))
			throw new InvalidOperationException("Hasło nie może być puste.");

		var salt = RandomNumberGenerator.GetBytes(SaltSize);
		var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
		return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
	}

	public static bool Verify(string password, string storedHash)
	{
		if (string.IsNullOrEmpty(storedHash)) return false;
		if (storedHash.StartsWith(Prefix + "$", StringComparison.Ordinal))
			return VerifyPbkdf2(password, storedHash);

		return VerifyLegacySha256(password, storedHash);
	}

	private static bool VerifyPbkdf2(string password, string storedHash)
	{
		var parts = storedHash.Split('$');
		if (parts.Length != 4) return false;
		if (!int.TryParse(parts[1], out var iterations)) return false;

		var salt = Convert.FromBase64String(parts[2]);
		var expected = Convert.FromBase64String(parts[3]);
		var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
		return CryptographicOperations.FixedTimeEquals(actual, expected);
	}

	private static bool VerifyLegacySha256(string password, string storedHash)
	{
		var passwordBytes = Encoding.UTF8.GetBytes(password);
		var hash = SHA256.HashData(passwordBytes);
		var actual = Convert.ToBase64String(hash);
		return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(actual), Encoding.UTF8.GetBytes(storedHash));
	}
}
