using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Apteka.Configuration;
using Apteka.Models;

namespace Apteka.Services;

public class BackupService(AppSettings settings)
{
	public string BackupDirectory => ResolveDirectory(settings.Backup.BackupDirectory);

	public IEnumerable<BackupFileEntry> ListBackups()
	{
		var directory = BackupDirectory;
		Directory.CreateDirectory(directory);

		return Directory.GetFiles(directory, "*.sql")
			.Select(path =>
			{
				var info = new FileInfo(path);
				return new BackupFileEntry
				{
					FileName = info.Name,
					FullPath = info.FullName,
					CreatedAt = info.CreationTime,
					SizeBytes = info.Length
				};
			})
			.OrderByDescending(x => x.CreatedAt)
			.ToList();
	}

	public async Task<string> CreateBackupAsync()
	{
		var directory = BackupDirectory;
		Directory.CreateDirectory(directory);

		var fileName = $"apteka_backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
		var path = Path.Combine(directory, fileName);
		var db = settings.Database;
		var credentials = GetManagerCredentials();

		var args = new List<string>
		{
			"-h", db.Host,
			"-p", db.Port.ToString(),
			"-U", credentials.Username,
			"--clean",
			"--if-exists",
			"--no-owner",
			"--no-privileges",
			"-f", path,
			db.Database
		};

		await RunPostgresToolAsync(ResolvePgDumpPath(), args, credentials.Password);
		return path;
	}

	public async Task RestoreBackupAsync(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
			throw new InvalidOperationException("Wybierz istniejący plik kopii zapasowej.");

		var db = settings.Database;
		var credentials = GetManagerCredentials();
		var args = new List<string>
		{
			"-h", db.Host,
			"-p", db.Port.ToString(),
			"-U", credentials.Username,
			"-d", db.Database,
			"-v", "ON_ERROR_STOP=1",
			"-f", path
		};

		await RunPostgresToolAsync(ResolvePsqlPath(), args, credentials.Password);
	}

	private DatabaseCredentials GetManagerCredentials()
	{
		if (settings.Database.RoleConnections.TryGetValue("kierownik", out var credentials))
			return credentials;

		return settings.Database.LoginCredentials;
	}

	private string ResolvePgDumpPath()
	{
		if (!string.Equals(settings.Backup.PgDumpPath, "auto", StringComparison.OrdinalIgnoreCase))
			return settings.Backup.PgDumpPath;

		const string homebrewPath = "/opt/homebrew/opt/postgresql@17/bin/pg_dump";
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && File.Exists(homebrewPath))
			return homebrewPath;

		return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "pg_dump.exe" : "pg_dump";
	}

	private string ResolvePsqlPath()
	{
		if (!string.Equals(settings.Backup.PsqlPath, "auto", StringComparison.OrdinalIgnoreCase))
			return settings.Backup.PsqlPath;

		const string homebrewPath = "/opt/homebrew/opt/postgresql@17/bin/psql";
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && File.Exists(homebrewPath))
			return homebrewPath;

		return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "psql.exe" : "psql";
	}

	private static string ResolveDirectory(string directory)
	{
		if (Path.IsPathRooted(directory))
			return directory;

		return Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
			"Apteka",
			directory);
	}

	private static async Task RunPostgresToolAsync(string fileName, IEnumerable<string> args, string password)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = fileName,
			UseShellExecute = false,
			RedirectStandardError = true,
			RedirectStandardOutput = true,
			CreateNoWindow = true
		};

		foreach (var arg in args)
			startInfo.ArgumentList.Add(arg);

		if (!string.IsNullOrEmpty(password))
			startInfo.Environment["PGPASSWORD"] = password;

		using var process = Process.Start(startInfo)
		                    ?? throw new InvalidOperationException($"Nie można uruchomić narzędzia {fileName}.");
		var output = await process.StandardOutput.ReadToEndAsync();
		var error = await process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync();

		if (process.ExitCode != 0)
			throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? output : error);
	}
}
