using System;
using Apteka.Models;

namespace Apteka.ViewModels.Grid;

public class WykonanieRecepturyViewModel(WykonanieReceptury wykonanie) : IFilterable
{
	public int Id => wykonanie.Id;
	public string NazwaReceptury => wykonanie.NazwaReceptury;
	public int? IdRecepty => wykonanie.IdRecepty;
	public int IdSprzedazy => wykonanie.IdSprzedazy;
	public DateTime DataWykonania => wykonanie.DataWykonania;
	public int Ilosc => wykonanie.Ilosc;
	public decimal KosztJednostkowy => wykonanie.KosztJednostkowy;

	public bool IsMatch(string searchText)
	{
		return NazwaReceptury.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || IdSprzedazy.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || (IdRecepty?.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false);
	}
}
