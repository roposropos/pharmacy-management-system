using System;
using System.Collections.Generic;
using System.Linq;
using Apteka.Models;
using Apteka.Repositories;
using Apteka.ViewModels.Grid;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Apteka.ViewModels;

public partial class ClientsViewModel : CrudViewModelBase<KlientViewModel>
{
	private readonly ClientRepository _clientRepository;
	private readonly Uzytkownik _uzytkownik;
	[ObservableProperty] private object? _currentDialogViewModel;

	public ClientsViewModel(ClientRepository clientRepository, Uzytkownik uzytkownik)
	{
		_clientRepository = clientRepository;
		_uzytkownik = uzytkownik;
		CanAdd = true;
		CanDelete = uzytkownik.Rola == "kierownik";
		CanEdit = uzytkownik.Rola == "kierownik";
		LoadData();
	}

	public bool CanViewSensitiveData => _uzytkownik.Rola == "kierownik";
	private bool HasUnsavedChanges => Items.Any(x => x.IsModified);
	private bool HasErrors => Items.Any(x => x.HasErrors);
	public event Action? BackRequested;

	[RelayCommand]
	protected sealed override void LoadData()
	{
		base.LoadData();
		var data = _clientRepository.GetAll();
		_allItems.Clear();
		foreach (var client in data)
		{
			var item = new KlientViewModel(client, CanViewSensitiveData);
			Items.Add(item);
			_allItems.Add(item);
		}
	}

	protected override void Add()
	{
		if (!CanAdd) return;
		var item = new KlientViewModel();
		Items.Add(item);
		_allItems.Add(item);
		SelectedItem = item;
		IsEditing = true;
	}

	[RelayCommand]
	public void GoBack()
	{
		if (HasUnsavedChanges) return;
		BackRequested?.Invoke();
	}

	protected sealed override void Save()
	{
		if (HasErrors) return;
		base.Save();
		if (HasDataToDelete)
		{
			foreach (var id in DeletedIds) _clientRepository.DeleteById(id);

			LoadData();
			return;
		}

		if (!HasUnsavedChanges) return;
		foreach (var modified in Items.Where(x => x.IsModified)) _clientRepository.AddOrUpdate(modified.Klient);
		LoadData();
	}

	[RelayCommand]
	private void CloseDialog()
	{
		CurrentDialogViewModel = null;
		SelectedItem?.IsModified = true;
		SelectedItem?.Reload();
	}

	public void EditPhoneNumbers()
	{
		if (SelectedItem == null) return;
		CurrentDialogViewModel = new PhonesViewModel(_uzytkownik, (List<Telefon>)SelectedItem.Klient.Osoba.Telefony);
	}

	public void EditAddress()
	{
		if (SelectedItem == null) return;
		CurrentDialogViewModel = new AddressViewModel(_uzytkownik, SelectedItem.Klient.Adres);
	}
}
