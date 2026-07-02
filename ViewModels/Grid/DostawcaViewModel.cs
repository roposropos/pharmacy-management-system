using System;
using Apteka.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apteka.ViewModels.Grid;

public partial class DostawcaViewModel : EditableViewModelBase, IFilterable
{
	[ObservableProperty] private Dostawca _dostawca;

	public DostawcaViewModel()
	{
		_dostawca = new Dostawca
		{
			Adres = new Adres
			{
				Kraj = "Polska"
			}
		};
		IsModified = true;
	}

	public DostawcaViewModel(Dostawca dostawca)
	{
		_dostawca = dostawca;
		_dostawca.Adres ??= new Adres
		{
			Kraj = "Polska"
		};
		IsModified = false;
	}

	public override int Id => Dostawca.Id;

	public string Nazwa
	{
		get => Dostawca.Nazwa;
		set
		{
			if (Dostawca.Nazwa == value) return;
			Dostawca.Nazwa = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public string NIP
	{
		get => Dostawca.NIP;
		set
		{
			if (Dostawca.NIP == value) return;
			Dostawca.NIP = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public string Kraj
	{
		get => Dostawca.Adres?.Kraj ?? string.Empty;
		set => UpdateAddressField(Dostawca.Adres?.Kraj, value, adres => adres.Kraj = value);
	}

	public string? Ulica
	{
		get => Dostawca.Adres?.Ulica;
		set => UpdateAddressField(Dostawca.Adres?.Ulica, value, adres => adres.Ulica = value);
	}

	public string NumerDomu
	{
		get => Dostawca.Adres?.NumerDomu ?? string.Empty;
		set => UpdateAddressField(Dostawca.Adres?.NumerDomu, value, adres => adres.NumerDomu = value);
	}

	public string? NumerLokalu
	{
		get => Dostawca.Adres?.NumerLokalu;
		set => UpdateAddressField(Dostawca.Adres?.NumerLokalu, value, adres => adres.NumerLokalu = value);
	}

	public string KodPocztowy
	{
		get => Dostawca.Adres?.KodPocztowy ?? string.Empty;
		set => UpdateAddressField(Dostawca.Adres?.KodPocztowy, value, adres => adres.KodPocztowy = value);
	}

	public string Miejscowosc
	{
		get => Dostawca.Adres?.Miejscowosc ?? string.Empty;
		set => UpdateAddressField(Dostawca.Adres?.Miejscowosc, value, adres => adres.Miejscowosc = value);
	}

	public string PelnyAdres => Dostawca.Adres?.PelnyAdres ?? string.Empty;

	public bool IsMatch(string searchText)
	{
		return Nazwa.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || NIP.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || PelnyAdres.Contains(searchText, StringComparison.OrdinalIgnoreCase);
	}

	private void UpdateAddressField(string? currentValue, string? newValue, Action<Adres> update)
	{
		Dostawca.Adres ??= new Adres
		{
			Kraj = "Polska"
		};

		if (currentValue == newValue) return;
		update(Dostawca.Adres);
		OnPropertyChanged();
		OnPropertyChanged(nameof(PelnyAdres));
		IsModified = true;
	}
}
