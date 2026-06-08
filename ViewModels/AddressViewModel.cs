using System;
using System.Linq;
using Apteka.Models;
using Apteka.ViewModels.Grid;
using CommunityToolkit.Mvvm.Input;

namespace Apteka.ViewModels;

public partial class AddressViewModel : CrudViewModelBase<AdresViewModel>
{
	private readonly Adres _adres;
	private readonly Adres _originalAdres;

	public AddressViewModel(Uzytkownik uzytkownik, Adres adres)
	{
		_originalAdres = adres;
		_adres = new Adres
		{
			Id = adres.Id,
			Kraj = adres.Kraj,
			Ulica = adres.Ulica,
			NumerDomu = adres.NumerDomu,
			NumerLokalu = adres.NumerLokalu,
			Miejscowosc = adres.Miejscowosc,
			KodPocztowy = adres.KodPocztowy
		};
		CanAdd = true;
		LoadData();
	}

	private bool HasUnsavedChanges => Items.Any(x => x.IsModified);
	private bool HasErrors => Items.Any(x => x.HasErrors);
	public event Action? BackRequested;

	[RelayCommand]
	protected sealed override void LoadData()
	{
		base.LoadData();
		_allItems.Clear();
		Items.Clear();
		var item = new AdresViewModel(_adres);
		Items.Add(item);
		_allItems.Add(item);
		SelectedItem = Items.First();
		IsEditing = true;
		OnPropertyChanged(nameof(SelectedItem));
	}

	protected sealed override void Cancel()
	{
		_adres.Kraj = _originalAdres.Kraj;
		_adres.Ulica = _originalAdres.Ulica;
		_adres.NumerDomu = _originalAdres.NumerDomu;
		_adres.NumerLokalu = _originalAdres.NumerLokalu;
		_adres.Miejscowosc = _originalAdres.Miejscowosc;
		_adres.KodPocztowy = _originalAdres.KodPocztowy;
		LoadData();
	}

	[RelayCommand]
	private void GoBack()
	{
		if (HasUnsavedChanges) return;
		BackRequested?.Invoke();
	}

	protected sealed override void Save()
	{
		if (HasErrors) return;
		base.Save();
		if (!HasUnsavedChanges) return;
		Console.WriteLine("Saving changes");

		_originalAdres.Kraj = _adres.Kraj;
		_originalAdres.Ulica = _adres.Ulica;
		_originalAdres.NumerDomu = _adres.NumerDomu;
		_originalAdres.NumerLokalu = _adres.NumerLokalu;
		_originalAdres.Miejscowosc = _adres.Miejscowosc;
		_originalAdres.KodPocztowy = _adres.KodPocztowy;
		LoadData();
	}
}