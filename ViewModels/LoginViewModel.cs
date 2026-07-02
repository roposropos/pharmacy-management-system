using System;
using Apteka.Configuration;
using Apteka.Models;
using Apteka.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Apteka.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
	private readonly AppSettings _settings;
	private readonly DatabaseService _databaseService;
	private readonly LoginRepository _loginRepository;

	[ObservableProperty] private string _connectionStatus = "Połączenie z bazą zostanie sprawdzone podczas logowania. Test połączenia jest opcjonalny.";
	[ObservableProperty] private string? _errorMessage;
	[ObservableProperty] private bool _isConnectionSettingsVisible;
	[ObservableProperty] private string _databaseName;
	[ObservableProperty] private string? _password;
	[ObservableProperty] private string _databasePort;
	[ObservableProperty] private string _databaseDriver;
	[ObservableProperty] private string _databaseHost;
	[ObservableProperty] private string _loginDatabasePassword;
	[ObservableProperty] private string _loginDatabaseUser;
	[ObservableProperty] private string? _username;

	public LoginViewModel(LoginRepository loginRepository, DatabaseService databaseService, AppSettings settings)
	{
		_loginRepository = loginRepository;
		_databaseService = databaseService;
		_settings = settings;
		_databaseHost = settings.Database.Host;
		_databasePort = settings.Database.Port.ToString();
		_databaseName = settings.Database.Database;
		_databaseDriver = settings.Database.Driver;
		_loginDatabaseUser = settings.Database.LoginUser;
		_loginDatabasePassword = settings.Database.LoginPassword;
	}

	public event Action<Uzytkownik>? LoginSuccessful;

	[RelayCommand]
	private void Login()
	{
		ErrorMessage = string.Empty;

		if (Username == null || Password == null)
		{
			ErrorMessage = "Uzupełnij login i hasło.";
			return;
		}

		try
		{
			ApplySettingsFromForm();
			var connectionCheck = _databaseService.CheckLoginConnection();
			ConnectionStatus = connectionCheck.Message;
			if (!connectionCheck.Success)
			{
				ErrorMessage = connectionCheck.Message;
				return;
			}

			var uzytkownik = _loginRepository.ValidateUser(Username, Password);

			if (uzytkownik != null)
			{
				LoginSuccessful?.Invoke(uzytkownik);
				return;
			}

			ErrorMessage = "Błędny login lub hasło";
		}
		catch (Exception ex)
		{
			ErrorMessage = DatabaseService.ToUserFriendlyConnectionMessage(ex);
			ConnectionStatus = "Połączenie z bazą nie działa.";
		}
	}

	[RelayCommand]
	private void ToggleConnectionSettings()
	{
		IsConnectionSettingsVisible = !IsConnectionSettingsVisible;
	}

	[RelayCommand]
	private void CheckConnection()
	{
		try
		{
			ApplySettingsFromForm();
			var result = _databaseService.CheckLoginConnection();
			ConnectionStatus = result.Message;
			ErrorMessage = result.Success ? string.Empty : result.Message;
		}
		catch (Exception ex)
		{
			ConnectionStatus = DatabaseService.ToUserFriendlyConnectionMessage(ex);
			ErrorMessage = ConnectionStatus;
		}
	}

	[RelayCommand]
	private void SaveConnectionSettings()
	{
		try
		{
			ApplySettingsFromForm();
			_settings.SaveLocal();
			_databaseService.UseLoginCredentials();
			ConnectionStatus = $"Zapisano konfigurację: {AppSettings.UserSettingsPath}";
			CheckConnection();
		}
		catch (Exception ex)
		{
			ConnectionStatus = $"Nie zapisano konfiguracji: {ex.Message}";
		}
	}

	private void ApplySettingsFromForm()
	{
		if (!int.TryParse(DatabasePort, out var port) || port <= 0)
			throw new InvalidOperationException("Port bazy danych musi być dodatnią liczbą.");

		_settings.Database.Host = DatabaseHost.Trim();
		_settings.Database.Port = port;
		_settings.Database.Database = DatabaseName.Trim();
		_settings.Database.Driver = string.IsNullOrWhiteSpace(DatabaseDriver) ? "auto" : DatabaseDriver.Trim();
		_settings.Database.LoginUser = LoginDatabaseUser.Trim();
		_settings.Database.LoginPassword = LoginDatabasePassword;
		_settings.Database.Normalize();
		_databaseService.UseLoginCredentials();
	}
}
