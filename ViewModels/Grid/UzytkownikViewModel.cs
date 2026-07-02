using System;
using Apteka.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apteka.ViewModels.Grid;

public partial class UzytkownikViewModel : EditableViewModelBase, IFilterable
{
	[ObservableProperty] private Uzytkownik _uzytkownik;

	public UzytkownikViewModel()
	{
		_uzytkownik = new Uzytkownik
		{
			Rola = "farmaceuta",
			Aktywny = true,
			Osoba = new Osoba()
		};
		IsModified = true;
	}

	public UzytkownikViewModel(Uzytkownik uzytkownik)
	{
		_uzytkownik = uzytkownik;
		_uzytkownik.Osoba ??= new Osoba();
		IsModified = false;
	}

	[ObservableProperty] private string _newPassword = string.Empty;

	public override int Id => Uzytkownik.Id;

	public string Login
	{
		get => Uzytkownik.Login;
		set
		{
			if (Uzytkownik.Login == value) return;
			Uzytkownik.Login = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public string Imie
	{
		get => Uzytkownik.Osoba.Imie;
		set
		{
			if (Uzytkownik.Osoba.Imie == value) return;
			Uzytkownik.Osoba.Imie = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(ImieNazwisko));
			IsModified = true;
		}
	}

	public string Nazwisko
	{
		get => Uzytkownik.Osoba.Nazwisko;
		set
		{
			if (Uzytkownik.Osoba.Nazwisko == value) return;
			Uzytkownik.Osoba.Nazwisko = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(ImieNazwisko));
			IsModified = true;
		}
	}

	public string ImieNazwisko => $"{Imie} {Nazwisko}".Trim();

	public string Rola
	{
		get => Uzytkownik.Rola;
		set
		{
			if (Uzytkownik.Rola == value) return;
			Uzytkownik.Rola = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public bool Aktywny
	{
		get => Uzytkownik.Aktywny;
		set
		{
			if (Uzytkownik.Aktywny == value) return;
			Uzytkownik.Aktywny = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public DateTime? OstatnieLogowanie => Uzytkownik.OstatnieLogowanie;

	partial void OnNewPasswordChanged(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
			IsModified = true;
	}

	public bool IsMatch(string searchText)
	{
		return Login.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || ImieNazwisko.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || Rola.Contains(searchText, StringComparison.OrdinalIgnoreCase);
	}
}
