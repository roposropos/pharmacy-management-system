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
	[ObservableProperty] private ObservableCollection<Zamowienie> _newOrderItems = new();

	[ObservableProperty] private ObservableCollection<Zamowienie> _orders = new();
	[ObservableProperty] private int _quantity = 1;
	[ObservableProperty] private LekiViewModel? _selectedDrug;
	[ObservableProperty] private WariantLeku? _selectedVariant;

	public OrdersViewModel(OrderRepository orderRepository, DrugRepository drugRepository, Uzytkownik uzytkownik)
	{
		_orderRepository = orderRepository;
		_drugRepository = drugRepository;
		_uzytkownik = uzytkownik;
		LoadOrders();
		LoadDrugs();
	}

	public event Action? BackRequested;

	[RelayCommand]
	private void GoBack()
	{
		BackRequested?.Invoke();
	}

	private void LoadOrders()
	{
		Orders = new ObservableCollection<Zamowienie>(_orderRepository.GetAll());
	}

	private void LoadDrugs()
	{
		var drugs = _drugRepository.GetAll();
		AvailableDrugs = new ObservableCollection<LekiViewModel>(drugs.Select(d => new LekiViewModel(d)));
	}

	[RelayCommand]
	private void AddItem()
	{
		if (SelectedVariant == null || Quantity <= 0) return;

		;
	}

	[RelayCommand]
	private void RemoveItem(object item)
	{
	}

	[RelayCommand]
	private void PlaceOrder()
	{
		if (NewOrderItems.Count == 0) return;

		var order = new Zamowienie
		{
			DataUtworzenia = DateTime.Now,
			Status = "Nowe"
		};

		_orderRepository.Add(order);
		NewOrderItems.Clear();
		LoadOrders();
	}
}