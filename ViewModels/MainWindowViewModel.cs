using System;
using Apteka.Models;
using Apteka.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
	private readonly AddressRepository _addressRepository;
	private readonly DatabaseService _dbService;
	private readonly PhoneRepository _phoneRepository;
	[ObservableProperty] private ViewModelBase _currentView;
	private Uzytkownik _uzytkownik;

	public MainWindowViewModel()
	{
		_dbService = App.Current.Services!.GetRequiredService<DatabaseService>();
		_addressRepository = App.Current.Services!.GetRequiredService<AddressRepository>();
		_phoneRepository = App.Current.Services!.GetRequiredService<PhoneRepository>();
		_uzytkownik = new Uzytkownik();
		var lr = new LoginRepository(_dbService);
		var loginViewModel = new LoginViewModel(lr);

		loginViewModel.LoginSuccessful += OnLoginSuccess;
		CurrentView = loginViewModel;
	}

	private void OnLoginSuccess(Uzytkownik uzytkownik)
	{
		Console.WriteLine($"Zalogowano: {uzytkownik.FullName}");
		_uzytkownik = uzytkownik;
		_dbService.UpdateCredentials(_uzytkownik.Rola);
		ShowDashboard();
	}

	private void ShowDashboard()
	{
		var dashboardVm = new DashboardViewModel(_uzytkownik);
		dashboardVm.NavigationRequested += NavigateToView;
		CurrentView = dashboardVm;
	}

	private void Navigate(ViewModelBase viewModel)
	{
		CurrentView = viewModel;
	}

	private void NavigateToView(string view)
	{
		switch (view)
		{
			case "Klienci":
				var clientsVm =
					new ClientsViewModel(new ClientRepository(_dbService, _addressRepository, _phoneRepository),
						_uzytkownik);
				clientsVm.BackRequested += ShowDashboard;
				CurrentView = clientsVm;
				break;
			case "Produkty":
				var drugsVm = new DrugsViewModel(new DrugRepository(_dbService, _addressRepository), _uzytkownik);
				drugsVm.BackRequested += ShowDashboard;
				CurrentView = drugsVm;
				break;
			case "Raporty":
				var reportsVm = new ReportsViewModel(new ReportsRepository(_dbService), _uzytkownik);
				reportsVm.BackRequested += ShowDashboard;
				CurrentView = reportsVm;
				break;
			case "Sprzedaż":
				var saleVm = new SaleViewModel(_uzytkownik, new DrugRepository(_dbService, _addressRepository));
				saleVm.BackRequested += ShowDashboard;
				CurrentView = saleVm;
				break;
			case "Wyloguj":
				_dbService.UpdateCredentials("postgres");
				var loginVm = new LoginViewModel(new LoginRepository(_dbService));
				loginVm.LoginSuccessful += OnLoginSuccess;
				CurrentView = loginVm;
				break;
			case "Recepty":
				var prescriptionsVm =
					new PrescriptionsViewModel(new DrugRepository(_dbService, _addressRepository), _uzytkownik);
				prescriptionsVm.BackRequested += ShowDashboard;
				prescriptionsVm.NavigateRequested += Navigate;
				CurrentView = prescriptionsVm;
				break;
			case "Zamówienia":
				var ordersVm = new OrdersViewModel(new OrderRepository(_dbService, _addressRepository),
					new DrugRepository(_dbService, _addressRepository), _uzytkownik);
				ordersVm.BackRequested += ShowDashboard;
				CurrentView = ordersVm;
				break;
			case "Dostawy":
				var deliveriesVm = new DeliveriesViewModel(new DeliveryRepository(_dbService, _addressRepository),
					new DrugRepository(_dbService, _addressRepository),
					new OrderRepository(_dbService, _addressRepository), _uzytkownik);
				deliveriesVm.BackRequested += ShowDashboard;
				CurrentView = deliveriesVm;
				break;
			default:
				Console.WriteLine($"View: {view}");
				break;
		}
	}
}