using System;
using System.Collections.ObjectModel;
using System.Linq;
using Apteka.Models;
using Apteka.Repositories;
using Apteka.ViewModels.Grid;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Apteka.ViewModels;

public partial class DeliveriesViewModel : CrudViewModelBase<LekiViewModel>
{
	private readonly DeliveryRepository _deliveryRepository;
	private readonly DrugRepository _drugRepository;
		private readonly OrderRepository _orderRepository;
		private readonly Uzytkownik _uzytkownik;
		[ObservableProperty] private ObservableCollection<LekiViewModel> _availableDrugs = new();
		[ObservableProperty] private ObservableCollection<Surowiec> _availableRawMaterials = new();
		[ObservableProperty] private ObservableCollection<Dostawca> _availableSuppliers = new();
		[ObservableProperty] private string _batchNumber = string.Empty;
		[ObservableProperty] private string _deliveryItemType = "Lek";

		[ObservableProperty] private ObservableCollection<PozycjaDostawy> _deliveryLines = new();
		[ObservableProperty] private ObservableCollection<Dostawa> _deliveries = new();
		[ObservableProperty] private DateTimeOffset _expiryDate = DateTimeOffset.Now.AddYears(2);
		[ObservableProperty] private ObservableCollection<DeliveryLineViewModel> _newDeliveryItems = new();
		[ObservableProperty] private ObservableCollection<Zamowienie> _pendingOrders = new();
		[ObservableProperty] private decimal _purchasePrice;

		[ObservableProperty] private decimal _quantity = 1;

		[ObservableProperty] private DeliveryLineViewModel? _selectedDeliveryItem;
		[ObservableProperty] private Dostawa? _selectedDelivery;
		[ObservableProperty] private LekiViewModel? _selectedDrug;
		[ObservableProperty] private Zamowienie? _selectedOrder;
		[ObservableProperty] private Surowiec? _selectedRawMaterial;
		[ObservableProperty] private Dostawca? _selectedSupplier;
		[ObservableProperty] private WariantLeku? _selectedVariant;
		[ObservableProperty] private string _statusMessage = string.Empty;


	public DeliveriesViewModel(DeliveryRepository deliveryRepository, DrugRepository drugRepository,
		OrderRepository orderRepository, Uzytkownik uzytkownik)
	{
		_deliveryRepository = deliveryRepository;
		_drugRepository = drugRepository;
		_orderRepository = orderRepository;
		_uzytkownik = uzytkownik;

			LoadDeliveries();
			LoadDrugs();
			LoadRawMaterials();
			LoadOrders();
			LoadSuppliers();
		}

		public string[] DeliveryItemTypes { get; } = ["Lek", "Surowiec"];
		public event Action? BackRequested;
		public bool CanManageDeliveries => _uzytkownik.Rola == "kierownik";

	[RelayCommand]
	private void GoBack()
	{
		BackRequested?.Invoke();
	}

	private void LoadDeliveries()
	{
		Deliveries = new ObservableCollection<Dostawa>(_deliveryRepository.GetAll());
		SelectedDelivery = Deliveries.FirstOrDefault(x => x.Id == SelectedDelivery?.Id) ?? Deliveries.FirstOrDefault();
	}

	private void LoadDrugs()
	{
		AvailableDrugs =
			new ObservableCollection<LekiViewModel>(_drugRepository.GetAll().Select(d => new LekiViewModel(d)));
	}

	private void LoadOrders()
	{
		PendingOrders = new ObservableCollection<Zamowienie>(_orderRepository.GetAll().Where(o => o.Status == "Nowe"));
	}

	private void LoadSuppliers()
	{
		AvailableSuppliers = new ObservableCollection<Dostawca>(_orderRepository.GetSuppliers());
		SelectedSupplier = AvailableSuppliers.FirstOrDefault();
	}

	private void LoadRawMaterials()
	{
		AvailableRawMaterials = new ObservableCollection<Surowiec>(_deliveryRepository.GetRawMaterials());
		SelectedRawMaterial = AvailableRawMaterials.FirstOrDefault();
	}

	[RelayCommand]
	private void AddItem()
	{
		if (Quantity <= 0 || string.IsNullOrWhiteSpace(BatchNumber)) return;

		if (DeliveryItemType == "Surowiec")
		{
			AddRawMaterialItem();
			return;
		}

		if (SelectedVariant == null) return;

		var name = $"{SelectedDrug?.Nazwa} {SelectedVariant.Dawka} x{SelectedVariant.Ilosc}";
		NewDeliveryItems.Add(new DeliveryLineViewModel
		{
			IdWariantu = SelectedVariant.Id,
			TypProduktu = "Lek",
			Nazwa = name,
			NumerPartii = BatchNumber.Trim(),
			DataWaznosci = ExpiryDate.DateTime,
			Ilosc = Quantity,
			CenaZakupu = PurchasePrice
		});

		BatchNumber = string.Empty;
		Quantity = 1;
		PurchasePrice = 0;
	}

	private void AddRawMaterialItem()
	{
		if (SelectedRawMaterial is null) return;

		NewDeliveryItems.Add(new DeliveryLineViewModel
		{
			IdSurowca = SelectedRawMaterial.Id,
			TypProduktu = "Surowiec",
			Nazwa = $"{SelectedRawMaterial.Nazwa} ({SelectedRawMaterial.Jednostka})",
			NumerPartii = BatchNumber.Trim(),
			DataWaznosci = ExpiryDate.DateTime,
			Ilosc = Quantity,
			CenaZakupu = PurchasePrice
		});

		BatchNumber = string.Empty;
		Quantity = 1;
		PurchasePrice = 0;
	}

	[RelayCommand]
	private void RemoveItem()
	{
		if (SelectedDeliveryItem == null) return;
		NewDeliveryItems.Remove(SelectedDeliveryItem);
	}

	[RelayCommand]
	private void SubmitDelivery()
	{
		if (SelectedSupplier == null || NewDeliveryItems.Count == 0) return;

		var delivery = new Dostawa
		{
			DataDostawy = DateTime.Now,
			IdDostawcy = SelectedSupplier.Id
		};

		try
		{
			_deliveryRepository.Add(delivery, NewDeliveryItems.Select(x => x.ToModel()));
			NewDeliveryItems.Clear();
			StatusMessage = "Dostawa została zapisana.";
			LoadDeliveries();
			LoadDrugs();
			LoadRawMaterials();
		}
		catch (Exception ex)
		{
			StatusMessage = $"Nie zapisano dostawy: {ex.Message}";
		}
	}

	partial void OnSelectedOrderChanged(Zamowienie? value)
	{
		if (value?.Dostawca == null) return;
		SelectedSupplier = AvailableSuppliers.FirstOrDefault(x => x.Id == value.Dostawca.Id) ?? SelectedSupplier;
	}

	[RelayCommand]
	private void DeleteDelivery()
	{
		if (!CanManageDeliveries || SelectedDelivery is null) return;

		try
		{
			_deliveryRepository.Delete(SelectedDelivery.Id);
			StatusMessage = "Dostawa została usunięta, a stany magazynowe cofnięte.";
			LoadDeliveries();
			LoadDrugs();
			LoadRawMaterials();
		}
		catch (Exception ex)
		{
			StatusMessage = $"Nie usunięto dostawy: {ex.Message}";
		}
	}

	partial void OnSelectedDeliveryChanged(Dostawa? value)
	{
		LoadDeliveryLines();
	}

	private void LoadDeliveryLines()
	{
		DeliveryLines = SelectedDelivery is null
			? new ObservableCollection<PozycjaDostawy>()
			: new ObservableCollection<PozycjaDostawy>(_deliveryRepository.GetLines(SelectedDelivery.Id));
	}
}
