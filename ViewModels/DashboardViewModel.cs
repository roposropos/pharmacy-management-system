using System;
using System.Windows.Input;
using Apteka.Models;
using CommunityToolkit.Mvvm.Input;

namespace Apteka.ViewModels;

public class DashboardViewModel : ViewModelBase
{
	private readonly Uzytkownik _uzytkownik;

	public DashboardViewModel(Uzytkownik uzytkownik)
	{
		_uzytkownik = uzytkownik;
		NavigateCommand = new RelayCommand<string>(Navigate);
	}

	public bool IsKierownik => _uzytkownik.Rola == "kierownik";
	public ICommand NavigateCommand { get; }
	public event Action<string>? NavigationRequested;

	private void Navigate(string? target)
	{
		if (target is null) return;
		NavigationRequested?.Invoke(target);
	}
}