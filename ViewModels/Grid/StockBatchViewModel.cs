using System;
using Apteka.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apteka.ViewModels.Grid;

public partial class StockBatchViewModel : ObservableObject, IFilterable
{
	[ObservableProperty] private StanPartiiLeku _batch;

	public StockBatchViewModel()
	{
		_batch = new StanPartiiLeku();
	}

	public StockBatchViewModel(StanPartiiLeku batch)
	{
		_batch = batch;
	}

	public int Id => Batch.IdPartii;
	public string Nazwa => Batch.PelnaNazwa;
	public string Producent => Batch.NazwaProducenta;
	public string SubstancjaCzynna => Batch.SubstancjaCzynna;
	public long KodEan => Batch.KodEan;
	public string NumerPartii => Batch.NumerPartii;
	public DateTime DataWaznosci => Batch.DataWaznosci;
	public int IloscDostepna => Batch.IloscDostepna;
	public int IloscZarezerwowana => Batch.IloscZarezerwowana;
	public int IloscDoSprzedazy => Batch.IloscDoSprzedazy;

	public bool IsMatch(string searchText)
	{
		return Nazwa.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || Producent.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || SubstancjaCzynna.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || NumerPartii.Contains(searchText, StringComparison.OrdinalIgnoreCase)
		       || KodEan.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase);
	}
}
