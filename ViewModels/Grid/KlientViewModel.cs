using System;
using System.Linq;
using Apteka.Models;
using Apteka.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apteka.ViewModels.Grid;

public partial class KlientViewModel : EditableViewModelBase, IFilterable
{
	[ObservableProperty] private Klient _klient;

	public KlientViewModel()
	{
		_klient = new Klient();
		_klient.Osoba ??= new Osoba();
		_klient.Adres ??= new Adres();
		ShowSensitiveData = true;
		ValidatePesel();
		IsModified = true;
	}

	public KlientViewModel(Klient klient, bool showSensitiveData = true)
	{
		_klient = klient;
		_klient.Osoba ??= new Osoba();
		_klient.Adres ??= new Adres();
		ShowSensitiveData = showSensitiveData;
		IsModified = false;
	}

	public override int Id => Klient.Id;
	public bool ShowSensitiveData { get; set; }

	public string Imie
	{
		get => Klient.Osoba.Imie;
		set
		{
			if (Klient.Osoba.Imie == value) return;
			Klient.Osoba.Imie = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(ImieNazwisko));
			IsModified = true;
		}
	}

	public string Nazwisko
	{
		get => Klient.Osoba.Nazwisko;
		set
		{
			if (Klient.Osoba.Nazwisko == value) return;
			Klient.Osoba.Nazwisko = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(ImieNazwisko));
			IsModified = true;
		}
	}

	public string ImieNazwisko => $"{Imie} {Nazwisko}";

	public string Pesel
	{
		get => Klient.Pesel;
		set
		{
			if (Klient.Pesel == value) return;
			if (!SetProperty(Klient.Pesel, value, Klient, (k, v) => k.Pesel = v)) return;
			ValidatePesel();
			OnPropertyChanged(nameof(HasErrors));
			OnPropertyChanged(nameof(PeselDisplay));
			IsModified = true;
		}
	}

	public string PeselDisplay
	{
		get => ShowSensitiveData || IsModified ? Pesel : PrivacyFormatter.MaskPesel(Pesel);
		set => Pesel = value;
	}

	public string Telefony
	{
		get
		{
			var phones = Klient.Osoba.Telefony;
			if (phones.Count == 0) return "Brak numeru";

			var fPhones = phones.Select(p =>
			{
				return !string.IsNullOrWhiteSpace(p.Opis) ? $"{p.Opis}: {p.Numer}" : $"{p.Numer}";
			});

			return string.Join(", ", fPhones);
		}
	}

	public string Adres => Klient.Adres.PelnyAdres;

	public bool IsMatch(string searchText)
	{
		return Imie.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || Nazwisko.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || Adres.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || Telefony.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || (ShowSensitiveData
			       ? Pesel.Contains(searchText, StringComparison.OrdinalIgnoreCase)
			       : PeselDisplay.Contains(searchText, StringComparison.OrdinalIgnoreCase));
	}

	private void ValidatePesel()
	{
		const string propertyName = nameof(Pesel);
		Errors.Remove(propertyName);

		if (!PeselValidator.IsValid(Pesel))
			Errors[propertyName] = ["Niepoprawny numer PESEL"];

		OnErrorsChanged(propertyName);
	}

	public void Reload()
	{
		OnPropertyChanged(nameof(Telefony));
		OnPropertyChanged(nameof(Adres));
		OnPropertyChanged(nameof(PeselDisplay));
	}
}
