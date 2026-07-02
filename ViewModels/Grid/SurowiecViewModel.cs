using System;
using Apteka.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apteka.ViewModels.Grid;

public partial class SurowiecViewModel : EditableViewModelBase, IFilterable
{
	[ObservableProperty] private Surowiec _surowiec;

	public SurowiecViewModel()
	{
		_surowiec = new Surowiec();
		IsModified = true;
	}

	public SurowiecViewModel(Surowiec surowiec)
	{
		_surowiec = surowiec;
		IsModified = false;
	}

	public override int Id => Surowiec.Id;

	public string Nazwa
	{
		get => Surowiec.Nazwa;
		set
		{
			if (Surowiec.Nazwa == value) return;
			Surowiec.Nazwa = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public string Typ
	{
		get => Surowiec.Typ;
		set
		{
			if (Surowiec.Typ == value) return;
			Surowiec.Typ = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public string Jednostka
	{
		get => Surowiec.Jednostka;
		set
		{
			if (Surowiec.Jednostka == value) return;
			Surowiec.Jednostka = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public decimal DostepnaIlosc => Surowiec.DostepnaIlosc;
	public DateTime? NajblizszaDataWaznosci => Surowiec.NajblizszaDataWaznosci;

	public bool IsMatch(string searchText)
	{
		return Nazwa.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || Typ.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || Jednostka.Contains(searchText, StringComparison.OrdinalIgnoreCase);
	}
}
