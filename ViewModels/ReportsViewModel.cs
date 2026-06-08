using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Apteka.Models;
using Apteka.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Apteka.ViewModels;

public partial class ReportsViewModel : ViewModelBase
{
	private readonly ReportsRepository _reportsRepository;
	private readonly Uzytkownik _uzytkownik;
	[ObservableProperty] private ObservableCollection<StanMagazynu> _drugStock = new();
	[ObservableProperty] private DateTimeOffset _endDate = DateTimeOffset.Now;

	[ObservableProperty] private ObservableCollection<Sprzedarz> _sales = new();
	[ObservableProperty] private int _selectedTabIndex;
	[ObservableProperty] private DateTimeOffset _startDate = DateTimeOffset.MinValue;

	public ReportsViewModel(ReportsRepository reportsRepository, Uzytkownik uzytkownik)
	{
		_reportsRepository = reportsRepository;
		_uzytkownik = uzytkownik;
		LoadData();
	}

	public event Action? BackRequested;

	[RelayCommand]
	private void LoadData()
	{
		Sales = new ObservableCollection<Sprzedarz>(_reportsRepository.GetSalesReport(StartDate, EndDate));
		DrugStock = new ObservableCollection<StanMagazynu>(_reportsRepository.GetDrugStock(StartDate, EndDate));
	}

	[RelayCommand]
	public void GoBack()
	{
		BackRequested?.Invoke();
	}

	[RelayCommand]
	private void ExportToCsv()
	{
		string fileName;
		string content;

		switch (SelectedTabIndex)
		{
			case 0:
				fileName = $"Raport_Sprzedazy_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
				content = GenerateSalesCsv();
				break;
			case 1:
				fileName = $"Raport_Magazynowy_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
				content = GenerateStockCsv();
				break;
			default:
				return;
		}

		try
		{
			File.WriteAllText(fileName, content, Encoding.UTF8);
			Console.WriteLine($"Wyeksportowano raport do: {fileName}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Błąd podczas eksportu: {ex.Message}");
		}
	}

	private string GenerateSalesCsv()
	{
		var sb = new StringBuilder();
		sb.AppendLine("Id;Data;Kwota Brutto");
		foreach (var item in Sales) sb.AppendLine($"{item.Id};{item.Data};{item.KwotaBrutto}");
		return sb.ToString();
	}

	private string GenerateStockCsv()
	{
		var sb = new StringBuilder();
		sb.AppendLine("Nazwa;Ilosc;TerminWaznosci;Typ");
		foreach (var item in DrugStock)
			sb.AppendLine($"{item.PelnaNazwa};{item.DostepnaIlosc};{item.DataWaznosci};{item.Typ}");
		return sb.ToString();
	}
}