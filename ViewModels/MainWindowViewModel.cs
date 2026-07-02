using System;
using Apteka.Configuration;
using Apteka.Models;
using Apteka.Repositories;
using Apteka.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
	private readonly AddressRepository _addressRepository;
	private readonly AppSettings _settings;
	private readonly DatabaseService _dbService;
	private readonly PhoneRepository _phoneRepository;
	private readonly SensitiveDataProtector _sensitiveDataProtector;
	[ObservableProperty] private ViewModelBase _currentView;
	private Uzytkownik _uzytkownik;

	public MainWindowViewModel()
	{
		_dbService = App.Current.Services!.GetRequiredService<DatabaseService>();
		_settings = App.Current.Services!.GetRequiredService<AppSettings>();
		_addressRepository = App.Current.Services!.GetRequiredService<AddressRepository>();
		_phoneRepository = App.Current.Services!.GetRequiredService<PhoneRepository>();
		_sensitiveDataProtector = App.Current.Services!.GetRequiredService<SensitiveDataProtector>();
		_uzytkownik = new Uzytkownik();
		var lr = new LoginRepository(_dbService);
		var loginViewModel = new LoginViewModel(lr, _dbService, _settings);

		loginViewModel.LoginSuccessful += OnLoginSuccess;
		CurrentView = loginViewModel;
	}

	private void OnLoginSuccess(Uzytkownik uzytkownik)
	{
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
					new ClientsViewModel(new ClientRepository(_dbService, _addressRepository, _phoneRepository,
							_sensitiveDataProtector),
						_uzytkownik);
				clientsVm.BackRequested += ShowDashboard;
				CurrentView = clientsVm;
				break;
			case "Produkty":
				var drugsVm = new DrugsViewModel(new DrugRepository(_dbService, _addressRepository),
					new SupplierRepository(_dbService), new InventoryRepository(_dbService), _uzytkownik);
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
				_dbService.UseLoginCredentials();
				var loginVm = new LoginViewModel(new LoginRepository(_dbService), _dbService, _settings);
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
			case "Pracownicy":
				var employeesVm = new EmployeesViewModel(new UserRepository(_dbService), _uzytkownik);
				employeesVm.BackRequested += ShowDashboard;
				CurrentView = employeesVm;
				break;
			default:
				break;
		}
	}
}
