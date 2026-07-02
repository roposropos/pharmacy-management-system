using System;
using Apteka.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apteka.ViewModels.Grid;

public partial class LekiViewModel : EditableViewModelBase, IFilterable
{
	[ObservableProperty] private Lek _lek;

	public LekiViewModel()
	{
		_lek = new Lek();
		_lek.Producent ??= new Producent();
		_lek.Producent.Adres ??= new Adres();
		IsModified = true;
	}

	public LekiViewModel(Lek lek)
	{
		_lek = lek;
		_lek.Producent ??= new Producent();
		_lek.Producent.Adres ??= new Adres();
		IsModified = false;
	}

	public override int Id => Lek.Id;

	public string Nazwa
	{
		get => Lek.Nazwa;
		set
		{
			if (Lek.Nazwa == value) return;
			Lek.Nazwa = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public string Producent
	{
		get => Lek.Producent.Nazwa;
		set
		{
			if (Lek.Producent.Nazwa == value) return;
			Lek.Producent.Nazwa = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public string AdresProducenta => Lek.Producent.Adres.PelnyAdres;

	public void ChangeProducer(Producent producent)
	{
		if (Lek.IdProducenta == producent.Id) return;
		Lek.IdProducenta = producent.Id;
		Lek.Producent = producent;
		OnPropertyChanged(nameof(Producent));
		OnPropertyChanged(nameof(AdresProducenta));
		IsModified = true;
	}

	public string SubstancjaCzynna
	{
		get => Lek.SubstancjaCzynna;
		set
		{
			if (Lek.SubstancjaCzynna == value) return;
			Lek.SubstancjaCzynna = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public bool BezRecepty
	{
		get => Lek.BezRecepty;
		set
		{
			if (Lek.BezRecepty == value) return;
			Lek.BezRecepty = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public bool IsMatch(string searchText)
	{
		return Nazwa.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || Producent.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || SubstancjaCzynna.Contains(searchText, StringComparison.OrdinalIgnoreCase);
	}
}
