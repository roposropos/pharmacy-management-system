using System;
using System.Data;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using Apteka.Configuration;

namespace Apteka.Repositories;

public class DatabaseService(AppSettings appSettings)
{
	private readonly DatabaseSettings _settings = appSettings.Database;
	private DatabaseCredentials _currentCredentials = appSettings.Database.LoginCredentials;
	private int? _currentUserId;

	public void UpdateCredentials(string username, string password = "")
	{
		if (_settings.RoleConnections.TryGetValue(username, out var mappedCredentials))
		{
			_currentCredentials = mappedCredentials;
			return;
		}

		_currentCredentials = new DatabaseCredentials
		{
			Username = username,
			Password = password
		};
	}

	public void UseLoginCredentials()
	{
		_currentCredentials = _settings.LoginCredentials;
		_currentUserId = null;
	}

	public void SetCurrentUser(int userId)
	{
		_currentUserId = userId;
	}

	/// <summary>
	///     Creates a connection to the database
	///     Sets app.user_id if presents
	/// </summary>
	/// <returns>Open IDbConnection</returns>
	public IDbConnection CreateConnection()
	{
		var connection = new OdbcConnection(BuildConnectionString());
		connection.Open();
		if (!_currentUserId.HasValue) return connection;

		using var command = connection.CreateCommand();
		command.CommandText = "SELECT set_config('app.user_id', ?, false);";
		command.Parameters.AddWithValue("@UserId", _currentUserId.ToString());
		command.ExecuteNonQuery();
		return connection;
	}

	public DatabaseConnectionCheck CheckLoginConnection()
	{
		try
		{
			UseLoginCredentials();
			using var connection = CreateConnection();
			using var command = connection.CreateCommand();
			command.CommandText = "SELECT current_database();";
			var database = command.ExecuteScalar()?.ToString() ?? _settings.Database;
			var schemaIssues = GetSchemaIssues(connection);
			if (schemaIssues.Length > 0)
				return new DatabaseConnectionCheck(
					false,
					$"Połączono z bazą {database}, ale schemat nie jest gotowy: {string.Join(", ", schemaIssues)}. Uruchom migracje z katalogu db/migrations.");

			return new DatabaseConnectionCheck(
				true,
				$"Połączono z bazą {database}. Schemat aplikacji jest gotowy. Sterownik: {ResolveDriver()}");
		}
		catch (OdbcException ex)
		{
			return new DatabaseConnectionCheck(false, ToUserFriendlyConnectionMessage(ex));
		}
		catch (System.Exception ex)
		{
			return new DatabaseConnectionCheck(false, ToUserFriendlyConnectionMessage(ex));
		}
	}

	public static string ToUserFriendlyConnectionMessage(Exception ex)
	{
		var rawMessage = FlattenMessage(ex);
		var normalized = rawMessage.ToLowerInvariant();

		if (normalized.Contains("libodbc.2.dylib") || normalized.Contains("unixodbc"))
			return "Nie można uruchomić połączenia ODBC na macOS. Użyj najnowszej paczki aplikacji albo zainstaluj brakujące biblioteki: brew install unixodbc psqlodbc.";

		if (normalized.Contains("psqlodbc") || normalized.Contains("postgresql unicode"))
			return "Nie znaleziono sterownika PostgreSQL ODBC. Zainstaluj psqlODBC albo ustaw sterownik w Konfiguracji połączenia.";

		if (normalized.Contains("connection refused") || normalized.Contains("could not connect") ||
		    normalized.Contains("is the server running"))
			return "Nie można połączyć się z PostgreSQL. Sprawdź, czy serwer bazy działa na podanym hoście i porcie.";

		if (normalized.Contains("password authentication failed"))
			return "Baza odrzuciła hasło użytkownika technicznego. Sprawdź konfigurację połączenia lub uruchom ponownie skrypt przygotowania bazy.";

		if (normalized.Contains("database") && normalized.Contains("does not exist"))
			return "Baza danych nie istnieje. Uruchom skrypt przygotowania bazy dla swojego systemu.";

		if (normalized.Contains("role") && normalized.Contains("does not exist"))
			return "Brakuje użytkownika technicznego PostgreSQL. Uruchom skrypt przygotowania bazy dla swojego systemu.";

		return $"Nie można połączyć się z lokalną bazą PostgreSQL. Sprawdź konfigurację połączenia. Szczegóły: {Shorten(rawMessage)}";
	}

	private static string[] GetSchemaIssues(IDbConnection connection)
	{
		var issues = new System.Collections.Generic.List<string>();
		var requiredTables = new[]
		{
			"uzytkownicy.uzytkownicy",
			"uzytkownicy.role",
			"apteka.klienci",
			"apteka.leki",
			"apteka.recepta",
			"apteka.receptury",
			"magazyn.partie_lekow",
			"magazyn.surowce",
			"magazyn.pozycje_zamowien",
			"magazyn.pozycje_dostaw"
		};

		foreach (var table in requiredTables.Where(table => !ObjectExists(connection, table)))
			issues.Add($"brak {table}");

		if (!ColumnExists(connection, "apteka", "klienci", "pesel_hash"))
			issues.Add("brak apteka.klienci.pesel_hash");
		if (!ColumnExists(connection, "magazyn", "pozycje_zamowien", "id_surowca_surowce"))
			issues.Add("brak zamówień surowców");
		if (!ColumnExists(connection, "magazyn", "pozycje_dostaw", "id_partii_surowca_partie_surowcow"))
			issues.Add("brak dostaw surowców");
		if (!TriggerExists(connection, "trg_audit_pozycje_zamowien"))
			issues.Add("brak pełnego audytu zamówień");
		if (!TriggerExists(connection, "trg_audit_partie_surowcow"))
			issues.Add("brak pełnego audytu surowców");

		return issues.ToArray();
	}

	private static bool ObjectExists(IDbConnection connection, string name)
	{
		using var command = connection.CreateCommand();
		command.CommandText = "SELECT to_regclass(?) IS NOT NULL;";
		var parameter = command.CreateParameter();
		parameter.ParameterName = "@ObjectName";
		parameter.Value = name;
		command.Parameters.Add(parameter);
		return ConvertDatabaseBoolean(command.ExecuteScalar());
	}

	private static bool TriggerExists(IDbConnection connection, string triggerName)
	{
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT EXISTS (
		                          SELECT 1
		                          FROM pg_trigger
		                          WHERE tgname = ?
		                            AND NOT tgisinternal
		                      );
		                      """;
		AddParameter(command, "@TriggerName", triggerName);
		return ConvertDatabaseBoolean(command.ExecuteScalar());
	}

	private static bool ColumnExists(IDbConnection connection, string schema, string table, string column)
	{
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT EXISTS (
		                          SELECT 1
		                          FROM pg_catalog.pg_attribute a
		                          JOIN pg_catalog.pg_class c ON c.oid = a.attrelid
		                          JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
		                          WHERE n.nspname = ?
		                            AND c.relname = ?
		                            AND a.attname = ?
		                            AND a.attnum > 0
		                            AND NOT a.attisdropped
		                      );
		                      """;
		AddParameter(command, "@Schema", schema);
		AddParameter(command, "@Table", table);
		AddParameter(command, "@Column", column);
		return ConvertDatabaseBoolean(command.ExecuteScalar());
	}

	private static void AddParameter(IDbCommand command, string name, object value)
	{
		var parameter = command.CreateParameter();
		parameter.ParameterName = name;
		parameter.Value = value;
		command.Parameters.Add(parameter);
	}

	private static bool ConvertDatabaseBoolean(object? value)
	{
		if (value is null || value == DBNull.Value) return false;
		return OdbcValue.ToBoolean(value);
	}

	private string BuildConnectionString()
	{
		var builder = new OdbcConnectionStringBuilder
		{
			["Driver"] = ResolveDriver(),
			["Server"] = _settings.Host,
			["Port"] = _settings.Port,
			["Database"] = _settings.Database,
			["Uid"] = _currentCredentials.Username,
			["Pwd"] = _currentCredentials.Password
		};

		return builder.ConnectionString;
	}

	private string ResolveDriver()
	{
		if (!string.Equals(_settings.Driver, "auto", System.StringComparison.OrdinalIgnoreCase) &&
		    !(IsCommonPostgreSqlOdbcDriverName(_settings.Driver) && !OperatingSystem.IsWindows()))
			return _settings.Driver;

		var bundledDriver = Path.Combine(AppContext.BaseDirectory, "psqlodbcw.so");
		if (File.Exists(bundledDriver))
			return bundledDriver;

		const string homebrewAppleSiliconDriver = "/opt/homebrew/lib/psqlodbcw.so";
		if (File.Exists(homebrewAppleSiliconDriver))
			return homebrewAppleSiliconDriver;

		const string homebrewIntelDriver = "/usr/local/lib/psqlodbcw.so";
		if (File.Exists(homebrewIntelDriver))
			return homebrewIntelDriver;

		const string linuxDriver = "/usr/lib/psqlodbcw.so";
		if (File.Exists(linuxDriver))
			return linuxDriver;

		const string linuxDriver64 = "/usr/lib/x86_64-linux-gnu/odbc/psqlodbcw.so";
		if (File.Exists(linuxDriver64))
			return linuxDriver64;

		const string windowsDriver = "PostgreSQL Unicode";
		return windowsDriver;
	}

	private static bool IsCommonPostgreSqlOdbcDriverName(string driver)
	{
		return driver.Equals("PostgreSQL Unicode", StringComparison.OrdinalIgnoreCase)
		       || driver.Equals("PostgreSQL ANSI", StringComparison.OrdinalIgnoreCase);
	}

	private static string FlattenMessage(Exception ex)
	{
		var messages = new System.Collections.Generic.List<string>();
		for (var current = ex; current != null; current = current.InnerException)
		{
			if (!string.IsNullOrWhiteSpace(current.Message))
				messages.Add(current.Message);
		}

		return string.Join(" ", messages);
	}

	private static string Shorten(string message)
	{
		var singleLine = message
			.Replace('\r', ' ')
			.Replace('\n', ' ')
			.Trim();

		const int maxLength = 220;
		if (singleLine.Length <= maxLength)
			return singleLine;

		return singleLine[..maxLength] + "...";
	}
}

public readonly record struct DatabaseConnectionCheck(bool Success, string Message);
