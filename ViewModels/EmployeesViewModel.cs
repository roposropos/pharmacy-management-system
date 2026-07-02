using System;
using System.Collections.ObjectModel;
using System.Linq;
using Apteka.Models;
using Apteka.Repositories;
using Apteka.ViewModels.Grid;
using CommunityToolkit.Mvvm.Input;

namespace Apteka.ViewModels;

public partial class EmployeesViewModel : CrudViewModelBase<UzytkownikViewModel>
{
	private readonly UserRepository _userRepository;
	private readonly Uzytkownik _currentUser;

	public EmployeesViewModel(UserRepository userRepository, Uzytkownik currentUser)
	{
		_userRepository = userRepository;
		_currentUser = currentUser;
		CanAdd = IsManager;
		CanEdit = IsManager;
		CanDelete = IsManager;
		LoadData();
	}

	public ObservableCollection<string> Roles { get; } = new();
	public bool IsManager => _currentUser.Rola == "kierownik";
	public string StatusMessage { get; private set; } = string.Empty;
	public event Action? BackRequested;

	[RelayCommand]
	protected sealed override void LoadData()
	{
		base.LoadData();
		_allItems.Clear();
		Roles.Clear();
		foreach (var role in _userRepository.GetRoles())
			Roles.Add(role);

		foreach (var user in _userRepository.GetAll())
		{
			var item = new UzytkownikViewModel(user);
			Items.Add(item);
			_allItems.Add(item);
		}

		SelectedItem = Items.FirstOrDefault();
	}

	protected override void Add()
	{
		if (!IsManager) return;
		var item = new UzytkownikViewModel();
		if (Roles.Count > 0 && !Roles.Contains(item.Rola))
			item.Rola = Roles.First();
		Items.Add(item);
		_allItems.Add(item);
		SelectedItem = item;
		IsEditing = true;
		SetStatus("Dodano nowe konto roboczo. Ustaw login, dane osoby i hasło.");
	}

	protected override void Delete()
	{
		if (!IsManager || SelectedItem is null) return;
		SelectedItem.Aktywny = false;
		IsEditing = true;
		SetStatus("Konto oznaczono jako nieaktywne. Zapisz zmiany, aby zablokować logowanie.");
	}

	protected override void Save()
	{
		if (!IsManager) return;

		try
		{
			foreach (var item in Items.Where(x => x.IsModified))
			{
				_userRepository.AddOrUpdate(item.Uzytkownik, item.NewPassword);
				item.NewPassword = string.Empty;
			}

			IsEditing = false;
			SetStatus("Konta pracowników zostały zapisane.");
			LoadData();
		}
		catch (Exception ex)
		{
			SetStatus($"Nie zapisano pracowników: {ex.Message}");
		}
	}

	protected override void Cancel()
	{
		IsEditing = false;
		SetStatus(string.Empty);
		LoadData();
	}

	[RelayCommand]
	public void GoBack()
	{
		BackRequested?.Invoke();
	}

	private void SetStatus(string message)
	{
		StatusMessage = message;
		OnPropertyChanged(nameof(StatusMessage));
	}
}
