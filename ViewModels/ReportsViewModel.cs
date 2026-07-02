using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apteka.Configuration;
using Apteka.Models;
using Apteka.Repositories;
using Apteka.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.ViewModels;

public partial class ReportsViewModel : ViewModelBase
{
	private const string RestoreConfirmationValue = "PRZYWROC";
	private readonly BackupService _backupService;
	private readonly ReportsRepository _reportsRepository;
	private readonly AppSettings _settings;
	private readonly Uzytkownik _uzytkownik;
	[ObservableProperty] private ObservableCollection<StanMagazynu> _alerts = new();
	[ObservableProperty] private ObservableCollection<AuditLogEntry> _auditLogs = new();
	[ObservableProperty] private ObservableCollection<BackupFileEntry> _backupFiles = new();
	[ObservableProperty] private string? _backupMessage;
	[ObservableProperty] private ObservableCollection<StanMagazynu> _drugStock = new();
	[ObservableProperty] private DateTimeOffset _endDate = DateTimeOffset.Now;
	[ObservableProperty] private string? _exportMessage;
	[ObservableProperty] private string _restoreConfirmation = string.Empty;

	[ObservableProperty] private ObservableCollection<Sprzedarz> _sales = new();
	[ObservableProperty] private BackupFileEntry? _selectedBackup;
	[ObservableProperty] private int _selectedTabIndex;
	[ObservableProperty] private DateTimeOffset _startDate = DateTimeOffset.MinValue;

	public ReportsViewModel(ReportsRepository reportsRepository, Uzytkownik uzytkownik)
	{
		_reportsRepository = reportsRepository;
		_uzytkownik = uzytkownik;
		_settings = App.Current.Services!.GetRequiredService<AppSettings>();
		_backupService = new BackupService(_settings);
		LoadData();
	}

	public string BackupDirectory => _backupService.BackupDirectory;
	public string RestoreConfirmationHint => $"Wpisz {RestoreConfirmationValue}, aby przywrócić kopię.";
	public event Action? BackRequested;

	[RelayCommand]
	private void LoadData()
	{
		Sales = new ObservableCollection<Sprzedarz>(_reportsRepository.GetSalesReport(StartDate, EndDate));
		DrugStock = new ObservableCollection<StanMagazynu>(_reportsRepository.GetDrugStock(StartDate, EndDate));
		Alerts = new ObservableCollection<StanMagazynu>(
			_reportsRepository.GetStockAlerts(10, DateTimeOffset.Now.AddDays(30)));
		AuditLogs = new ObservableCollection<AuditLogEntry>(_reportsRepository.GetAuditLog(StartDate, EndDate));
		LoadBackupFiles();
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
			case 2:
				fileName = $"Alerty_Magazynowe_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
				content = GenerateAlertsCsv();
				break;
			case 3:
				fileName = $"Dziennik_Audytu_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
				content = GenerateAuditCsv();
				break;
			default:
				return;
		}

		try
		{
			var path = GetExportPath(fileName);
			File.WriteAllText(path, content, Encoding.UTF8);
			ExportMessage = $"Wyeksportowano: {path}";
		}
		catch (Exception ex)
		{
			ExportMessage = $"Błąd eksportu: {ex.Message}";
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

	private string GenerateAlertsCsv()
	{
		var sb = new StringBuilder();
		sb.AppendLine("Nazwa;Ilosc;TerminWaznosci;Typ");
		foreach (var item in Alerts)
			sb.AppendLine($"{item.PelnaNazwa};{item.DostepnaIlosc};{item.DataWaznosci};{item.Typ}");
		return sb.ToString();
	}

	private string GenerateAuditCsv()
	{
		var sb = new StringBuilder();
		sb.AppendLine("Id;Data;Typ;Encja;Klucz;Login;Uzytkownik;Opis");
		foreach (var item in AuditLogs)
			sb.AppendLine(
				$"{item.Id};{item.DataOperacji};{item.TypOperacji};{item.Encja};{item.KluczRekordu};{item.Login};{item.Uzytkownik};{item.Opis}");
		return sb.ToString();
	}

	[RelayCommand]
	private async Task CreateBackup()
	{
		BackupMessage = "Tworzenie kopii...";
		try
		{
			var path = await _backupService.CreateBackupAsync();
			BackupMessage = $"Utworzono kopię: {path}";
			LoadBackupFiles();
		}
		catch (Exception ex)
		{
			BackupMessage = $"Błąd tworzenia kopii: {ex.Message}";
		}
	}

	[RelayCommand]
	private async Task RestoreBackup()
	{
		if (SelectedBackup is null)
		{
			BackupMessage = "Wybierz kopię do przywrócenia.";
			return;
		}

		if (!string.Equals(RestoreConfirmation, RestoreConfirmationValue, StringComparison.Ordinal))
		{
			BackupMessage = RestoreConfirmationHint;
			return;
		}

		BackupMessage = "Przywracanie kopii...";
		try
		{
			await _backupService.RestoreBackupAsync(SelectedBackup.FullPath);
			BackupMessage = "Kopia została przywrócona. Uruchom aplikację ponownie.";
			RestoreConfirmation = string.Empty;
			LoadData();
		}
		catch (Exception ex)
		{
			BackupMessage = $"Błąd przywracania kopii: {ex.Message}";
		}
	}

	[RelayCommand]
	private void RefreshBackups()
	{
		LoadBackupFiles();
	}

	private void LoadBackupFiles()
	{
		BackupFiles = new ObservableCollection<BackupFileEntry>(_backupService.ListBackups());
		SelectedBackup ??= BackupFiles.FirstOrDefault();
	}

	private string GetExportPath(string fileName)
	{
		var directory = _settings.Reports.ExportDirectory;
		if (!Path.IsPathRooted(directory))
			directory = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				"Apteka",
				directory);

		Directory.CreateDirectory(directory);
		return Path.Combine(directory, fileName);
	}
}
