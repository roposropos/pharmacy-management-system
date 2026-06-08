using System;
using System.Data;
using System.Data.Odbc;

namespace Apteka.Repositories;

public class DatabaseService
{
	private readonly string _driver = ReadEnv("APTEKA_DB_DRIVER", "PostgreSQL Unicode");
	private readonly string _server = ReadEnv("APTEKA_DB_HOST", "localhost");
	private readonly string _port = ReadEnv("APTEKA_DB_PORT", "5432");
	private readonly string _database = ReadEnv("APTEKA_DB_NAME", "Apteka");
	private readonly string _defaultUsername = ReadEnv("APTEKA_DB_USER", "postgres");
	private readonly string _defaultPassword = ReadEnv("APTEKA_DB_PASSWORD", "change-me");

	private string _connectionArgs;
	private int? _currentUserId;

	public DatabaseService()
	{
		_connectionArgs = BuildConnectionString(_defaultUsername, _defaultPassword);
	}

	public void UpdateCredentials(string username, string? password = null)
	{
		_connectionArgs = BuildConnectionString(username, password ?? _defaultPassword);
	}

	public void SetCurrentUser(int userId)
	{
		_currentUserId = userId;
	}

	/// <summary>
	///     Creates a connection to the database and sets app.user_id when available.
	/// </summary>
	public IDbConnection CreateConnection()
	{
		var connection = new OdbcConnection(_connectionArgs);
		connection.Open();
		if (!_currentUserId.HasValue) return connection;

		using var command = connection.CreateCommand();
		command.CommandText = "SELECT set_config('app.user_id', ?, false);";
		command.Parameters.AddWithValue("@UserId", _currentUserId.ToString());
		command.ExecuteNonQuery();
		return connection;
	}

	private string BuildConnectionString(string username, string password)
	{
		return $"Driver={{{_driver}}};Server={_server};Port={_port};Database={_database};Uid={username};Pwd={password};";
	}

	private static string ReadEnv(string name, string fallback)
	{
		return Environment.GetEnvironmentVariable(name) ?? fallback;
	}
}
