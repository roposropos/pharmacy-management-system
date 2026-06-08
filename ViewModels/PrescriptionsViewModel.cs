using System;
using System.Linq;
using Apteka.Models;
using Apteka.Repositories;
using Apteka.ViewModels.Grid;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.ViewModels;

public partial class PrescriptionsViewModel : CrudViewModelBase<ReceptaViewModel>
{
	private readonly DrugRepository _drugRepository;
	private readonly PrescriptionRepository _prescriptionRepository;
	private readonly Uzytkownik _uzytkownik;

	public PrescriptionsViewModel(DrugRepository drugRepository, Uzytkownik uzytkownik)
	{
		_drugRepository = drugRepository;
		_prescriptionRepository =
			new PrescriptionRepository(App.Current.Services!.GetRequiredService<DatabaseService>());
		_uzytkownik = uzytkownik;
		CanAdd = uzytkownik.Rola == "kierownik";
		CanDelete = uzytkownik.Rola == "kierownik";
		CanEdit = true;
		LoadData();
	}

	private bool HasUnsavedChanges => Items.Any(x => x.IsModified);
	public event Action<ViewModelBase>? NavigateRequested;
	public event Action? BackRequested;

	[RelayCommand]
	protected sealed override void LoadData()
	{
		base.LoadData();
		_allItems.Clear();
		var data = _prescriptionRepository.GetAll();
		foreach (var recepta in data)
		{
			var item = new ReceptaViewModel(recepta);
			Items.Add(item);
			_allItems.Add(item);
		}
	}

	[RelayCommand]
	public void Realize()
	{
		if (SelectedItem == null) return;
		//if (SelectedItem.DataRealizacji != null) return;
		var vm = new SaleViewModel(SelectedItem.Recepta, _uzytkownik, _drugRepository);
		vm.BackRequested += () =>
		{
			NavigateRequested?.Invoke(this);
			LoadData();
		};
		NavigateRequested?.Invoke(vm);
	}

	[RelayCommand]
	public void FindPrevious()
	{
		var newItem = Items.FirstOrDefault(x => x.Id == SelectedItem?.PoprzedniaReceptaId);
		if (newItem == null) return;
		SelectedItem = newItem;
	}

	[RelayCommand]
	public void GoBack()
	{
		if (HasUnsavedChanges) return;
		BackRequested?.Invoke();
	}
}