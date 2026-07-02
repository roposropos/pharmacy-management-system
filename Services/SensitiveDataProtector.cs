using System;
using System.Security.Cryptography;
using System.Text;
using Apteka.Configuration;

namespace Apteka.Services;

public sealed class SensitiveDataProtector
{
	private const string Prefix = "enc:v1:";
	private const int NonceSize = 12;
	private const int TagSize = 16;
	private readonly byte[] _key;

	public SensitiveDataProtector(AppSettings settings)
	{
		_key = SHA256.HashData(Encoding.UTF8.GetBytes(settings.Security.SensitiveDataKey));
	}

	public bool IsProtected(string? value)
	{
		return value?.StartsWith(Prefix, StringComparison.Ordinal) == true;
	}

	public string Protect(string? value)
	{
		var normalized = Normalize(value);
		if (string.IsNullOrEmpty(normalized) || IsProtected(normalized))
			return normalized;

		var nonce = RandomNumberGenerator.GetBytes(NonceSize);
		var plaintext = Encoding.UTF8.GetBytes(normalized);
		var ciphertext = new byte[plaintext.Length];
		var tag = new byte[TagSize];

		using var aes = new AesGcm(_key, TagSize);
		aes.Encrypt(nonce, plaintext, ciphertext, tag);

		return string.Join(':',
			Prefix.TrimEnd(':'),
			Convert.ToBase64String(nonce),
			Convert.ToBase64String(tag),
			Convert.ToBase64String(ciphertext));
	}

	public string Unprotect(string? value)
	{
		var stored = Normalize(value);
		if (string.IsNullOrEmpty(stored) || !IsProtected(stored))
			return stored;

		var parts = stored.Split(':');
		if (parts.Length != 5 || parts[0] != "enc" || parts[1] != "v1")
			throw new InvalidOperationException("Niepoprawny format zaszyfrowanych danych klienta.");

		try
		{
			var nonce = Convert.FromBase64String(parts[2]);
			var tag = Convert.FromBase64String(parts[3]);
			var ciphertext = Convert.FromBase64String(parts[4]);
			var plaintext = new byte[ciphertext.Length];

			using var aes = new AesGcm(_key, TagSize);
			aes.Decrypt(nonce, ciphertext, tag, plaintext);
			return Encoding.UTF8.GetString(plaintext);
		}
		catch (Exception ex) when (ex is FormatException or CryptographicException)
		{
			throw new InvalidOperationException(
				"Nie można odszyfrować danych klienta. Sprawdź klucz APTEKA_SENSITIVE_DATA_KEY lub ustawienie Security:SensitiveDataKey.",
				ex);
		}
	}

	public string Hash(string? value)
	{
		var normalized = Normalize(value);
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
	}

	private static string Normalize(string? value)
	{
		return value?.Trim() ?? string.Empty;
	}
}
