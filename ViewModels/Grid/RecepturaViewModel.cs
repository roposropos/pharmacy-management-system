using System;
using System.Collections.ObjectModel;
using System.Linq;
using Apteka.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apteka.ViewModels.Grid;

public partial class RecepturaViewModel : EditableViewModelBase, IFilterable
{
	[ObservableProperty] private Receptura _receptura;

	public RecepturaViewModel()
	{
		_receptura = new Receptura();
		Skladniki = new ObservableCollection<RecepturaSkladnikViewModel>();
		IsModified = true;
	}

	public RecepturaViewModel(Receptura receptura)
	{
		_receptura = receptura;
		Skladniki = new ObservableCollection<RecepturaSkladnikViewModel>(
			receptura.Skladniki.Select(x => new RecepturaSkladnikViewModel(x)));
		IsModified = false;
	}

	public override int Id => Receptura.Id;
	public ObservableCollection<RecepturaSkladnikViewModel> Skladniki { get; }

	public string Nazwa
	{
		get => Receptura.Nazwa;
		set
		{
			if (Receptura.Nazwa == value) return;
			Receptura.Nazwa = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public string Opis
	{
		get => Receptura.Opis;
		set
		{
			if (Receptura.Opis == value) return;
			Receptura.Opis = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public bool Zatwierdzona
	{
		get => Receptura.Zatwierdzona;
		set
		{
			if (Receptura.Zatwierdzona == value) return;
			Receptura.Zatwierdzona = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public decimal KosztPrzygotowania
	{
		get => Receptura.KosztPrzygotowania;
		set
		{
			if (Receptura.KosztPrzygotowania == value) return;
			Receptura.KosztPrzygotowania = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public string SkladOpis => string.Join(", ", Skladniki.Select(x => x.Opis));

	public Receptura ToModel()
	{
		Receptura.Skladniki = Skladniki.Select(x => x.ToModel()).ToList();
		return Receptura;
	}

	public void AddIngredient(Surowiec surowiec, decimal amount)
	{
		var existing = Skladniki.FirstOrDefault(x => x.IdSurowca == surowiec.Id);
		if (existing is not null)
		{
			existing.Ilosc += amount;
		}
		else
		{
			Skladniki.Add(new RecepturaSkladnikViewModel(new RecepturaSkladnik
			{
				IdReceptury = Id,
				IdSurowca = surowiec.Id,
				NazwaSurowca = surowiec.Nazwa,
				Jednostka = surowiec.Jednostka,
				Ilosc = amount
			}));
		}

		OnPropertyChanged(nameof(SkladOpis));
		IsModified = true;
	}

	public bool IsMatch(string searchText)
	{
		return Nazwa.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || Opis.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || SkladOpis.Contains(searchText, StringComparison.OrdinalIgnoreCase);
	}
}
