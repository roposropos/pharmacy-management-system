using System;
using Apteka.Models;
using Apteka.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Apteka.ViewModels;

public partial class LoginViewModel(LoginRepository loginRepository) : ViewModelBase
{
	[ObservableProperty] private string? _errorMessage;
	[ObservableProperty] private string? _password;
	[ObservableProperty] private string? _username;
	public event Action<Uzytkownik>? LoginSuccessful;

	[RelayCommand]
	private void Login()
	{
		ErrorMessage = string.Empty;

		if (Username == null || Password == null)
		{
			ErrorMessage = "Uzupełnij oba pola";
			return;
		}

		var uzytkownik = loginRepository.ValidateUser(Username, Password);

		if (uzytkownik != null)
		{
			LoginSuccessful?.Invoke(uzytkownik);
			return;
		}

		ErrorMessage = "Błędny login lub hasło";
	}
}