using System;
using System.Collections.ObjectModel;
using System.Linq;
using Apteka.Models;
using Apteka.Repositories;
using Apteka.ViewModels.Grid;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Apteka.ViewModels;

public partial class OrdersViewModel : ViewModelBase
{
	private readonly DrugRepository _drugRepository;
		private readonly OrderRepository _orderRepository;
		private readonly Uzytkownik _uzytkownik;
		[ObservableProperty] private ObservableCollection<LekiViewModel> _availableDrugs = new();
		[ObservableProperty] private ObservableCollection<Surowiec> _availableRawMaterials = new();
		[ObservableProperty] private ObservableCollection<Dostawca> _availableSuppliers = new();
		[ObservableProperty] private decimal _estimatedPrice;
		[ObservableProperty] private decimal _minimumStockLevel = 10;
		[ObservableProperty] private ObservableCollection<OrderLineViewModel> _newOrderItems = new();

		[ObservableProperty] private ObservableCollection<PozycjaZamowienia> _orderLines = new();
		[ObservableProperty] private ObservableCollection<Zamowienie> _orders = new();
		[ObservableProperty] private string _orderItemType = "Lek";
		[ObservableProperty] private decimal _quantity = 1;
		[ObservableProperty] private LekiViewModel? _selectedDrug;
		[ObservableProperty] private OrderLineViewModel? _selectedNewOrderItem;
		[ObservableProperty] private Zamowienie? _selectedOrder;
		[ObservableProperty] private PozycjaZamowienia? _selectedOrderLine;
		[ObservableProperty] private Surowiec? _selectedRawMaterial;
		[ObservableProperty] private Dostawca? _selectedSupplier;
		[ObservableProperty] private WariantLeku? _selectedVariant;
		[ObservableProperty] private string _statusMessage = string.Empty;
		[ObservableProperty] private decimal _targetStockLevel = 30;

	public OrdersViewModel(OrderRepository orderRepository, DrugRepository drugRepository, Uzytkownik uzytkownik)
	{
		_orderRepository = orderRepository;
		_drugRepository = drugRepository;
		_uzytkownik = uzytkownik;
			LoadOrders();
			LoadDrugs();
			LoadRawMaterials();
			LoadSuppliers();
		}

		public string[] OrderItemTypes { get; } = ["Lek", "Surowiec"];
		public event Action? BackRequested;
		public bool CanManageOrders => _uzytkownik.Rola == "kierownik";

	[RelayCommand]
	private void GoBack()
	{
		BackRequested?.Invoke();
	}

	private void LoadOrders()
	{
		Orders = new ObservableCollection<Zamowienie>(_orderRepository.GetAll());
		SelectedOrder = Orders.FirstOrDefault(x => x.Id == SelectedOrder?.Id) ?? Orders.FirstOrDefault();
	}

	private void LoadDrugs()
	{
		var drugs = _drugRepository.GetAll();
		AvailableDrugs = new ObservableCollection<LekiViewModel>(drugs.Select(d => new LekiViewModel(d)));
	}

	private void LoadSuppliers()
	{
		AvailableSuppliers = new ObservableCollection<Dostawca>(_orderRepository.GetSuppliers());
		SelectedSupplier = AvailableSuppliers.FirstOrDefault();
	}

	private void LoadRawMaterials()
	{
		AvailableRawMaterials = new ObservableCollection<Surowiec>(_orderRepository.GetRawMaterials());
		SelectedRawMaterial = AvailableRawMaterials.FirstOrDefault();
	}

	[RelayCommand]
	private void AddItem()
	{
		if (Quantity <= 0) return;

		if (OrderItemType == "Surowiec")
		{
			AddRawMaterialItem();
			return;
		}

		if (SelectedVariant == null) return;

		var name = $"{SelectedDrug?.Nazwa} {SelectedVariant.Dawka} x{SelectedVariant.Ilosc}";
		var existing = NewOrderItems.FirstOrDefault(x => x.TypProduktu == "Lek" && x.IdWariantu == SelectedVariant.Id);
		if (existing != null)
		{
			existing.Ilosc += Quantity;
			if (EstimatedPrice > 0) existing.CenaSzacowana = EstimatedPrice;
			return;
		}

		NewOrderItems.Add(new OrderLineViewModel
		{
			IdWariantu = SelectedVariant.Id,
			TypProduktu = "Lek",
			Nazwa = name,
			Ilosc = Quantity,
			CenaSzacowana = EstimatedPrice
		});
	}

	private void AddRawMaterialItem()
	{
		if (SelectedRawMaterial is null) return;

		var name = $"{SelectedRawMaterial.Nazwa} ({SelectedRawMaterial.Jednostka})";
		var existing = NewOrderItems.FirstOrDefault(x => x.TypProduktu == "Surowiec" && x.IdSurowca == SelectedRawMaterial.Id);
		if (existing != null)
		{
			existing.Ilosc += Quantity;
			if (EstimatedPrice > 0) existing.CenaSzacowana = EstimatedPrice;
			return;
		}

		NewOrderItems.Add(new OrderLineViewModel
		{
			IdSurowca = SelectedRawMaterial.Id,
			TypProduktu = "Surowiec",
			Nazwa = name,
			Ilosc = Quantity,
			CenaSzacowana = EstimatedPrice
		});
	}

	[RelayCommand]
	private void RemoveItem()
	{
		if (SelectedNewOrderItem == null) return;
		NewOrderItems.Remove(SelectedNewOrderItem);
	}

	[RelayCommand]
	private void AddReorderSuggestions()
	{
		try
		{
			var suggestions = _orderRepository.GetReorderSuggestions(MinimumStockLevel, TargetStockLevel).ToList();
			if (suggestions.Count == 0)
			{
				StatusMessage = "Nie znaleziono braków magazynowych dla podanych progów.";
				return;
			}

			foreach (var suggestion in suggestions)
				MergeNewOrderItem(new OrderLineViewModel
				{
					IdWariantu = suggestion.IdWariantu,
					IdSurowca = suggestion.IdSurowca,
					TypProduktu = suggestion.TypProduktu,
					Nazwa = suggestion.Nazwa,
					Ilosc = suggestion.Ilosc,
					CenaSzacowana = suggestion.CenaSzacowana
				});

			StatusMessage = $"Dodano propozycje uzupełnienia zapasów: {suggestions.Count}.";
		}
		catch (Exception ex)
		{
			StatusMessage = $"Nie dodano propozycji zamówienia: {ex.Message}";
		}
	}

	[RelayCommand]
	private void PlaceOrder()
	{
		if (NewOrderItems.Count == 0 || SelectedSupplier == null) return;

		var order = new Zamowienie
			{
				DataUtworzenia = DateTime.Now,
				Status = "Nowe",
				Typ = ResolveOrderType(),
				IdDostawcy = SelectedSupplier.Id
			};

		try
		{
			_orderRepository.Add(order, NewOrderItems.Select(x => x.ToModel()));
			NewOrderItems.Clear();
			Quantity = 1;
			EstimatedPrice = 0;
			StatusMessage = "Zamówienie zostało złożone.";
			LoadOrders();
		}
		catch (Exception ex)
		{
			StatusMessage = $"Nie złożono zamówienia: {ex.Message}";
		}
	}

	private string ResolveOrderType()
	{
		var hasDrugs = NewOrderItems.Any(x => x.TypProduktu == "Lek");
		var hasRawMaterials = NewOrderItems.Any(x => x.TypProduktu == "Surowiec");
		return (hasDrugs, hasRawMaterials) switch
		{
			(true, true) => "Mieszane",
			(false, true) => "Surowiec",
			_ => "Lek"
		};
	}

	private void MergeNewOrderItem(OrderLineViewModel item)
	{
		var existing = NewOrderItems.FirstOrDefault(x =>
			x.TypProduktu == item.TypProduktu
			&& x.IdWariantu == item.IdWariantu
			&& x.IdSurowca == item.IdSurowca);

		if (existing is null)
		{
			NewOrderItems.Add(item);
			return;
		}

		if (existing.Ilosc < item.Ilosc)
			existing.Ilosc = item.Ilosc;
	}

	[RelayCommand]
	private void ApproveOrder()
	{
		ChangeStatus("Zatwierdzone");
	}

	[RelayCommand]
	private void MarkOrderRealized()
	{
		ChangeStatus("Zrealizowane");
	}

	[RelayCommand]
	private void ArchiveOrder()
	{
		ChangeStatus("Archiwalne");
	}

	[RelayCommand]
	private void CancelOrder()
	{
		ChangeStatus("Anulowane");
	}

	[RelayCommand]
	private void DeleteOrderLine()
	{
		if (!CanManageOrders || SelectedOrderLine is null) return;

		try
		{
			_orderRepository.DeleteLine(SelectedOrderLine.Id);
			StatusMessage = "Pozycja zamówienia została usunięta.";
			LoadOrderLines();
		}
		catch (Exception ex)
		{
			StatusMessage = $"Nie usunięto pozycji: {ex.Message}";
		}
	}

	[RelayCommand]
	private void DeleteOrder()
	{
		if (!CanManageOrders || SelectedOrder is null) return;

		try
		{
			_orderRepository.Delete(SelectedOrder.Id);
			StatusMessage = "Zamówienie zostało usunięte.";
			LoadOrders();
		}
		catch (Exception ex)
		{
			StatusMessage = $"Nie usunięto zamówienia: {ex.Message}";
		}
	}

	private void ChangeStatus(string status)
	{
		if (!CanManageOrders || SelectedOrder is null) return;

		try
		{
			var orderId = SelectedOrder.Id;
			_orderRepository.UpdateStatus(orderId, status);
			StatusMessage = $"Status zamówienia zmieniono na: {status}.";
			LoadOrders();
			SelectedOrder = Orders.FirstOrDefault(x => x.Id == orderId);
		}
		catch (Exception ex)
		{
			StatusMessage = $"Nie zmieniono statusu: {ex.Message}";
		}
	}

	partial void OnSelectedOrderChanged(Zamowienie? value)
	{
		LoadOrderLines();
	}

	private void LoadOrderLines()
	{
		OrderLines = SelectedOrder is null
			? new ObservableCollection<PozycjaZamowienia>()
			: new ObservableCollection<PozycjaZamowienia>(_orderRepository.GetLines(SelectedOrder.Id));
		SelectedOrderLine = OrderLines.FirstOrDefault();
	}
}
