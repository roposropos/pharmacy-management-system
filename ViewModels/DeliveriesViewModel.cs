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
	[ObservableProperty] private string _batchNumber = string.Empty;

	[ObservableProperty] private ObservableCollection<Dostawa> _deliveries = new();
	[ObservableProperty] private DateTimeOffset _expiryDate = DateTimeOffset.Now.AddYears(2);
	[ObservableProperty] private ObservableCollection<Zamowienie> _pendingOrders = new();

	[ObservableProperty] private int _quantity = 1;

	[ObservableProperty] private LekiViewModel? _selectedDrug;
	[ObservableProperty] private Zamowienie? _selectedOrder;
	[ObservableProperty] private WariantLeku? _selectedVariant;


	public DeliveriesViewModel(DeliveryRepository deliveryRepository, DrugRepository drugRepository,
		OrderRepository orderRepository, Uzytkownik uzytkownik)
	{
		_deliveryRepository = deliveryRepository;
		_drugRepository = drugRepository;
		_orderRepository = orderRepository;
		_uzytkownik = uzytkownik;

		LoadDeliveries();
		LoadDrugs();
		LoadOrders();
	}

	public event Action? BackRequested;

	[RelayCommand]
	private void GoBack()
	{
		BackRequested?.Invoke();
	}

	private void LoadDeliveries()
	{
		Deliveries = new ObservableCollection<Dostawa>(_deliveryRepository.GetAll());
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

	[RelayCommand]
	private void AddItem()
	{
		if (SelectedVariant == null || Quantity <= 0 || string.IsNullOrWhiteSpace(BatchNumber)) return;

		var item = new Dostawa();
	}

	[RelayCommand]
	private void RemoveItem(Dostawa item)
	{
	}

	[RelayCommand]
	private void SubmitDelivery()
	{
		LoadDeliveries();
	}
}