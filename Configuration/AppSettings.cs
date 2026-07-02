using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Apteka.Configuration;

public sealed class AppSettings
{
	public DatabaseSettings Database { get; set; } = new();
	public ReportsSettings Reports { get; set; } = new();
	public BackupSettings Backup { get; set; } = new();
	public SecuritySettings Security { get; set; } = new();
	public static string UserSettingsPath => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"Apteka",
		"appsettings.local.json");

	public static AppSettings Load()
	{
		foreach (var path in CandidatePaths())
		{
			if (!File.Exists(path)) continue;

			var json = File.ReadAllText(path);
			var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});

			if (settings == null) continue;
			settings.Normalize();
			return settings;
		}

		var fallback = new AppSettings();
		fallback.Normalize();
		return fallback;
	}

	public void SaveLocal()
	{
		Normalize();
		var directory = Path.GetDirectoryName(UserSettingsPath);
		if (!string.IsNullOrWhiteSpace(directory))
			Directory.CreateDirectory(directory);

		var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
		{
			WriteIndented = true
		});
		File.WriteAllText(UserSettingsPath, json);
	}

	private static IEnumerable<string> CandidatePaths()
	{
		yield return UserSettingsPath;
		yield return Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");
		yield return Path.Combine(AppContext.BaseDirectory, "appsettings.json");
		yield return Path.Combine(Environment.CurrentDirectory, "appsettings.local.json");
		yield return Path.Combine(Environment.CurrentDirectory, "appsettings.json");
	}

	private void Normalize()
	{
		Database.Normalize();
		Reports.Normalize();
		Backup.Normalize();
		Security.Normalize();
	}
}

public sealed class SecuritySettings
{
	public const string DefaultSensitiveDataKey = "Apteka-demo-sensitive-data-key-change-in-local-settings";

	public string SensitiveDataKey { get; set; } = DefaultSensitiveDataKey;

	public void Normalize()
	{
		var environmentKey = Environment.GetEnvironmentVariable("APTEKA_SENSITIVE_DATA_KEY");
		if (!string.IsNullOrWhiteSpace(environmentKey))
			SensitiveDataKey = environmentKey;

		if (string.IsNullOrWhiteSpace(SensitiveDataKey))
			SensitiveDataKey = DefaultSensitiveDataKey;
	}
}

public sealed class DatabaseSettings
{
	public string Driver { get; set; } = "auto";
	public string Host { get; set; } = "localhost";
	public int Port { get; set; } = 5432;
	public string Database { get; set; } = "Apteka";
	public string LoginUser { get; set; } = "apteka_app";
	public string LoginPassword { get; set; } = "apteka_app";
	public Dictionary<string, DatabaseCredentials> RoleConnections { get; set; } = new();

	public DatabaseCredentials LoginCredentials => new()
	{
		Username = LoginUser,
		Password = LoginPassword
	};

	public void Normalize()
	{
		RoleConnections = new Dictionary<string, DatabaseCredentials>(RoleConnections, StringComparer.OrdinalIgnoreCase);
		RoleConnections.TryAdd("farmaceuta", new DatabaseCredentials
		{
			Username = "apteka_farmaceuta",
			Password = "farmaceuta"
		});
		RoleConnections.TryAdd("kierownik", new DatabaseCredentials
		{
			Username = "apteka_kierownik",
			Password = "kierownik"
		});
	}
}

public sealed class DatabaseCredentials
{
	public string Username { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
}

public sealed class ReportsSettings
{
	public string ExportDirectory { get; set; } = "exports";

	public void Normalize()
	{
		if (string.IsNullOrWhiteSpace(ExportDirectory))
			ExportDirectory = "exports";
	}
}

public sealed class BackupSettings
{
	public string BackupDirectory { get; set; } = "backups";
	public string PgDumpPath { get; set; } = "auto";
	public string PsqlPath { get; set; } = "auto";

	public void Normalize()
	{
		if (string.IsNullOrWhiteSpace(BackupDirectory))
			BackupDirectory = "backups";
		if (string.IsNullOrWhiteSpace(PgDumpPath))
			PgDumpPath = "auto";
		if (string.IsNullOrWhiteSpace(PsqlPath))
			PsqlPath = "auto";
	}
}
