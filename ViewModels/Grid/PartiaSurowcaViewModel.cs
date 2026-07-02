using System;
using Apteka.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apteka.ViewModels.Grid;

public partial class PartiaSurowcaViewModel : ObservableObject, IFilterable
{
	[ObservableProperty] private PartiaSurowca _partia;

	public PartiaSurowcaViewModel()
	{
		_partia = new PartiaSurowca();
	}

	public PartiaSurowcaViewModel(PartiaSurowca partia)
	{
		_partia = partia;
	}

	public int Id => Partia.Id;
	public string NazwaSurowca => Partia.NazwaSurowca;
	public string Jednostka => Partia.Jednostka;
	public string NumerPartii => Partia.NumerPartii;
	public DateTime DataWaznosci => Partia.DataWaznosci;
	public decimal IloscDostepna => Partia.IloscDostepna;
	public decimal IloscZarezerwowana => Partia.IloscZarezerwowana;
	public decimal IloscDoUzycia => Partia.IloscDoUzycia;

	public bool IsMatch(string searchText)
	{
		return NazwaSurowca.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || NumerPartii.Contains(searchText, StringComparison.OrdinalIgnoreCase);
	}
}
