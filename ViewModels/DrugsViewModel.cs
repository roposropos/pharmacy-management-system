using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Apteka.Models;
using Apteka.Repositories;
using Apteka.ViewModels.Grid;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Apteka.ViewModels;

public partial class DrugsViewModel : CrudViewModelBase<LekiViewModel>
{
	private readonly DrugRepository _drugRepository;
	private readonly InventoryRepository _inventoryRepository;
	private readonly SupplierRepository _supplierRepository;
	private readonly Uzytkownik _uzytkownik;

	[ObservableProperty] private ObservableCollection<StockBatchViewModel> _batches = new();
	[ObservableProperty] private string _inventoryMessage = string.Empty;
	[ObservableProperty] private bool _isSupplierEditing;
	[ObservableProperty] private ObservableCollection<Producent> _producers = new();
	[ObservableProperty] private StockBatchViewModel? _selectedBatch;
	[ObservableProperty] private Producent? _selectedProducer;
	[ObservableProperty] private DostawcaViewModel? _selectedSupplier;
	[ObservableProperty] private WariantLeku? _selectedVariant;
	[ObservableProperty] private string _statusMessage = string.Empty;
	[ObservableProperty] private int _stockAdjustmentQuantity;
	[ObservableProperty] private string _stockAdjustmentReason = string.Empty;
	[ObservableProperty] private ObservableCollection<DostawcaViewModel> _suppliers = new();
	[ObservableProperty] private string _supplierMessage = string.Empty;
	[ObservableProperty] private string _variantDawka = string.Empty;
	[ObservableProperty] private int _variantIlosc = 1;
	[ObservableProperty] private long _variantKodEan;
	[ObservableProperty] private string _variantMessage = string.Empty;
	[ObservableProperty] private PostacLeku _variantPostac;

	public DrugsViewModel(DrugRepository drugRepository, SupplierRepository supplierRepository,
		InventoryRepository inventoryRepository, Uzytkownik uzytkownik)
	{
		_drugRepository = drugRepository;
		_supplierRepository = supplierRepository;
		_inventoryRepository = inventoryRepository;
		_uzytkownik = uzytkownik;

		CanAdd = CanManageCatalog;
		CanEdit = CanManageCatalog;
		CanDelete = CanManageCatalog;

		LoadData();
	}

	public PostacLeku[] AvailableForms { get; } = Enum.GetValues<PostacLeku>();
	public bool CanManageCatalog => _uzytkownik.Rola == "kierownik";

	private bool HasUnsavedChanges => Items.Any(x => x.IsModified) || Suppliers.Any(x => x.IsModified);
	public event Action? BackRequested;

	[RelayCommand]
	protected sealed override void LoadData()
	{
		base.LoadData();
		LoadProducers();
		LoadProducts();
		LoadSuppliers();
		LoadBatches();
	}

	protected override void Add()
	{
		if (!CanManageCatalog) return;
		var producer = SelectedProducer ?? Producers.FirstOrDefault();
		var item = new LekiViewModel(new Lek
		{
			Producent = producer ?? new Producent(),
			IdProducenta = producer?.Id ?? 0
		});
		Items.Add(item);
		_allItems.Add(item);
		SelectedItem = item;
		IsEditing = true;
		StatusMessage = "Dodano nowy lek roboczo. Uzupełnij dane i zapisz.";
	}

	protected override void Cancel()
	{
		StatusMessage = string.Empty;
		VariantMessage = string.Empty;
		base.Cancel();
	}

	protected override void Save()
	{
		if (!CanManageCatalog) return;

		try
		{
			foreach (var id in DeletedIds.Distinct())
				_drugRepository.Delete(id);

			foreach (var item in Items.Where(x => x.IsModified))
				_drugRepository.AddOrUpdate(item.Lek);

			IsEditing = false;
			StatusMessage = "Katalog leków został zapisany.";
			LoadData();
		}
		catch (Exception ex)
		{
			StatusMessage = $"Nie zapisano katalogu: {ex.Message}";
		}
	}

	[RelayCommand]
	public void GoBack()
	{
		if (HasUnsavedChanges) return;
		BackRequested?.Invoke();
	}

	[RelayCommand]
	private void NewVariant()
	{
		SelectedVariant = null;
		VariantKodEan = 0;
		VariantDawka = string.Empty;
		VariantIlosc = 1;
		VariantPostac = PostacLeku.Tabletka;
		VariantMessage = "Przygotowano nowy wariant.";
	}

	[RelayCommand]
	private void SaveVariant()
	{
		if (!CanManageCatalog) return;
		if (SelectedItem is null)
		{
			VariantMessage = "Wybierz lek, dla którego chcesz zapisać wariant.";
			return;
		}

		try
		{
			var productId = SelectedItem.Id;
			var variant = new WariantLeku
			{
				Id = SelectedVariant?.Id ?? 0,
				KodEan = VariantKodEan,
				Dawka = VariantDawka,
				Ilosc = VariantIlosc,
				Postac = VariantPostac
			};

			_drugRepository.AddOrUpdateVariant(productId, variant);
			VariantMessage = "Wariant został zapisany.";
			LoadData();
			SelectedItem = Items.FirstOrDefault(x => x.Id == productId);
		}
		catch (Exception ex)
		{
			VariantMessage = $"Nie zapisano wariantu: {ex.Message}";
		}
	}

	[RelayCommand]
	private void DeleteVariant()
	{
		if (!CanManageCatalog || SelectedVariant is null) return;

		try
		{
			var productId = SelectedItem?.Id ?? 0;
			_drugRepository.DeleteVariant(SelectedVariant.Id);
			VariantMessage = "Wariant został usunięty.";
			LoadData();
			SelectedItem = Items.FirstOrDefault(x => x.Id == productId);
		}
		catch (Exception ex)
		{
			VariantMessage = $"Nie usunięto wariantu: {ex.Message}";
		}
	}

	[RelayCommand]
	private void ApplyStockAdjustment()
	{
		if (!CanManageCatalog) return;
		if (SelectedBatch is null)
		{
			InventoryMessage = "Wybierz partię do korekty.";
			return;
		}

		try
		{
			var selectedBatchId = SelectedBatch.Id;
			var newQuantity = _inventoryRepository.AdjustBatchQuantity(
				selectedBatchId,
				StockAdjustmentQuantity,
				StockAdjustmentReason);
			InventoryMessage = $"Korekta zapisana. Nowy stan dostępny: {newQuantity}.";
			StockAdjustmentQuantity = 0;
			StockAdjustmentReason = string.Empty;
			LoadBatches();
			LoadProducts();
			SelectedBatch = Batches.FirstOrDefault(x => x.Id == selectedBatchId);
		}
		catch (Exception ex)
		{
			InventoryMessage = $"Nie zapisano korekty: {ex.Message}";
		}
	}

	[RelayCommand]
	private void AddSupplier()
	{
		if (!CanManageCatalog) return;
		var supplier = new DostawcaViewModel();
		Suppliers.Add(supplier);
		SelectedSupplier = supplier;
		IsSupplierEditing = true;
		SupplierMessage = "Dodano nowego dostawcę roboczo. Uzupełnij dane i zapisz.";
	}

	[RelayCommand]
	private void EditSupplier()
	{
		if (!CanManageCatalog || SelectedSupplier is null) return;
		IsSupplierEditing = true;
	}

	[RelayCommand]
	private void SaveSupplier()
	{
		if (!CanManageCatalog || SelectedSupplier is null) return;

		try
		{
			_supplierRepository.AddOrUpdate(SelectedSupplier.Dostawca);
			IsSupplierEditing = false;
			SupplierMessage = "Dostawca został zapisany.";
			LoadSuppliers();
		}
		catch (Exception ex)
		{
			SupplierMessage = $"Nie zapisano dostawcy: {ex.Message}";
		}
	}

	[RelayCommand]
	private void DeleteSupplier()
	{
		if (!CanManageCatalog || SelectedSupplier is null) return;

		try
		{
			if (SelectedSupplier.Id == 0)
			{
				Suppliers.Remove(SelectedSupplier);
				SelectedSupplier = null;
			}
			else
			{
				_supplierRepository.Delete(SelectedSupplier.Id);
				LoadSuppliers();
			}

			IsSupplierEditing = false;
			SupplierMessage = "Dostawca został usunięty.";
		}
		catch (Exception ex)
		{
			SupplierMessage = $"Nie usunięto dostawcy: {ex.Message}";
		}
	}

	[RelayCommand]
	private void CancelSupplier()
	{
		IsSupplierEditing = false;
		SupplierMessage = string.Empty;
		LoadSuppliers();
	}

	protected override void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);
		if (e.PropertyName == nameof(SelectedItem))
			HandleSelectedItemChanged(SelectedItem);
	}

	private void HandleSelectedItemChanged(LekiViewModel? value)
	{
		if (value is null)
		{
			SelectedProducer = null;
			SelectedVariant = null;
			return;
		}

		SelectedProducer = Producers.FirstOrDefault(x => x.Id == value.Lek.IdProducenta) ?? value.Lek.Producent;
		SelectedVariant = value.Lek.Warianty.FirstOrDefault();
	}

	partial void OnSelectedProducerChanged(Producent? value)
	{
		if (value is null || SelectedItem is null || !IsEditing) return;
		SelectedItem.ChangeProducer(value);
	}

	partial void OnSelectedVariantChanged(WariantLeku? value)
	{
		if (value is null) return;
		VariantKodEan = value.KodEan;
		VariantDawka = value.Dawka;
		VariantIlosc = value.Ilosc;
		VariantPostac = value.Postac;
	}

	private void LoadProducts()
	{
		Items.Clear();
		_allItems.Clear();
		foreach (var lek in _drugRepository.GetAll())
		{
			var item = new LekiViewModel(lek);
			Items.Add(item);
			_allItems.Add(item);
		}
	}

	private void LoadProducers()
	{
		Producers = new ObservableCollection<Producent>(_drugRepository.GetProducers());
	}

	private void LoadSuppliers()
	{
		Suppliers = new ObservableCollection<DostawcaViewModel>(
			_supplierRepository.GetAll().Select(x => new DostawcaViewModel(x)));
		SelectedSupplier = Suppliers.FirstOrDefault();
	}

	private void LoadBatches()
	{
		Batches = new ObservableCollection<StockBatchViewModel>(
			_inventoryRepository.GetDrugBatches().Select(x => new StockBatchViewModel(x)));
		SelectedBatch = Batches.FirstOrDefault();
	}
}
