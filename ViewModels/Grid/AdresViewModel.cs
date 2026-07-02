using System;
using System.Runtime.CompilerServices;
using Apteka.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apteka.ViewModels.Grid;

public partial class AdresViewModel : EditableViewModelBase, IFilterable
{
	[ObservableProperty] private Adres _adres;

	public AdresViewModel()
	{
		_adres = new Adres();
		ValidateAdres(Kraj);
		ValidateAdres(NumerDomu);
		ValidateAdres(Miejscowosc);
		ValidateAdres(KodPocztowy);
		IsModified = true;
	}

	public AdresViewModel(Adres adres)
	{
		_adres = adres;
		ValidateAll();
		IsModified = false;
	}

	public override int Id => Adres.Id;

	public string Kraj
	{
		get => Adres.Kraj;
		set
		{
			if (Adres.Kraj == value) return;
			Adres.Kraj = value;
			OnPropertyChanged();
			IsModified = true;
			ValidateAdres(Kraj);
		}
	}

	public string Ulica
	{
		get => Adres.Ulica ?? string.Empty;
		set
		{
			if (Adres.Ulica == value) return;
			Adres.Ulica = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public string NumerDomu
	{
		get => Adres.NumerDomu;
		set
		{
			if (Adres.NumerDomu == value) return;
			Adres.NumerDomu = value;
			OnPropertyChanged();
			IsModified = true;
			ValidateAdres(NumerDomu);
		}
	}

	public string NumerLokalu
	{
		get => Adres.NumerLokalu ?? string.Empty;
		set
		{
			if (Adres.NumerLokalu == value) return;
			Adres.NumerLokalu = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public string Miejscowosc
	{
		get => Adres.Miejscowosc;
		set
		{
			if (Adres.Miejscowosc == value) return;
			Adres.Miejscowosc = value;
			OnPropertyChanged();
			IsModified = true;
			ValidateAdres(Miejscowosc);
		}
	}

	public string KodPocztowy
	{
		get => Adres.KodPocztowy;
		set
		{
			if (Adres.KodPocztowy == value) return;
			Adres.KodPocztowy = value;
			OnPropertyChanged();
			IsModified = true;
			ValidateAdres(KodPocztowy);
		}
	}

	public bool IsMatch(string searchText)
	{
		return Ulica.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || Miejscowosc.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || KodPocztowy.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || Kraj.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || NumerDomu.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || NumerLokalu.Contains(searchText, StringComparison.OrdinalIgnoreCase);
	}

	private void ValidateAll()
	{
		ValidateAdres(Kraj, nameof(Kraj));
		ValidateAdres(NumerDomu, nameof(NumerDomu));
		ValidateAdres(Miejscowosc, nameof(Miejscowosc));
		ValidateAdres(KodPocztowy, nameof(KodPocztowy));
	}

	private void ValidateAdres(string value, [CallerMemberName] string propertyName = "")
	{
		Errors.Remove(propertyName);

		if (string.IsNullOrWhiteSpace(value))
			Errors[propertyName] = [$"Pole \"{propertyName}\" nie może być puste"];

		OnErrorsChanged(propertyName);
	}
}