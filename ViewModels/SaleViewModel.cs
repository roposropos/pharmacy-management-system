using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Apteka.Models;
using Apteka.Repositories;
using Apteka.ViewModels.Grid;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.ViewModels;

public partial class SaleViewModel : CrudViewModelBase<LekiViewModel>
{
	private readonly List<PozycjaSprzedazyViewModel> _allShoppingItems = [];
	private readonly DrugRepository _drugRepository;
	private readonly Recepta? _recepta;
	private readonly SaleRepository _saleRepository;
	private readonly Uzytkownik _uzytkownik;
	[ObservableProperty] private object? _currentDialogViewModel;
	[ObservableProperty] private string? _errorMessage;
	[ObservableProperty] private bool _isPrescription;
	[ObservableProperty] private PartiaLeku? _partiaLeku;
	[ObservableProperty] private Sprzedarz _sprzedarz;

	private WariantLeku? _wariantLeku;

	public SaleViewModel(Recepta recepta, Uzytkownik uzytkownik, DrugRepository drugRepository)
	{
		_recepta = recepta;
		_uzytkownik = uzytkownik;
		_drugRepository = drugRepository;
		_sprzedarz = new Sprzedarz();
		_isPrescription = true;
		var dbService = App.Current.Services!.GetRequiredService<DatabaseService>();
		_saleRepository = new SaleRepository(dbService);
		LoadData();
	}

	public SaleViewModel(Uzytkownik uzytkownik, DrugRepository drugRepository)
	{
		_uzytkownik = uzytkownik;
		_drugRepository = drugRepository;
		_sprzedarz = new Sprzedarz();
		_isPrescription = false;
		var dbService = App.Current.Services!.GetRequiredService<DatabaseService>();
		_saleRepository = new SaleRepository(dbService);
		LoadData();
	}

	public ObservableCollection<PozycjaSprzedazyViewModel> ShoppingItems { get; } = [];

	public WariantLeku? WariantLeku
	{
		get;
		set
		{
			field = value;
			if (!SetProperty(ref _wariantLeku, value)) return;
			if (value != null) OnVariantChanged(value);
		}
	}

	private bool HasUnsavedChanges => false;

	public event Action? BackRequested;

	[RelayCommand]
	public void GoBack()
	{
		if (HasUnsavedChanges) return;
		BackRequested?.Invoke();
	}

	[RelayCommand]
	protected sealed override void LoadData()
	{
		base.LoadData();
		_allItems.Clear();
		var data = _recepta is null
			? _drugRepository.GetNoPrescription()
			: _drugRepository.GetFromPrescription(_recepta.Id);
		foreach (var lek in data)
		{
			var item = new LekiViewModel(lek);
			Items.Add(item);
			_allItems.Add(item);
		}
	}

	private void OnVariantChanged(WariantLeku wariant)
	{
		PartiaLeku = wariant.PartieLekow
			.Where(p => p.IloscLaczna > 0 && p.DataWaznosci.Date >= DateTime.Today)
			.OrderBy(p => p.DataWaznosci)
			.FirstOrDefault();
	}

	private string FullProductName()
	{
		return $"{SelectedItem?.Nazwa} {WariantLeku?.Dawka}x{WariantLeku?.Ilosc} - {PartiaLeku?.NumerPartii}";
	}

	[RelayCommand]
	public void AddItem()
	{
		if (SelectedItem is null || WariantLeku is null) return;
		if (PartiaLeku is null)
		{
			ErrorMessage = "Brak dostępnej, ważnej partii wybranego wariantu.";
			return;
		}

		if (PartiaLeku.IloscLaczna <= 0)
		{
			var nextBatch = WariantLeku?.PartieLekow
				.Where(p => p.Id != PartiaLeku.Id && p.IloscLaczna > 0 && p.DataWaznosci.Date >= DateTime.Today)
				.OrderBy(p => p.DataWaznosci)
				.FirstOrDefault();

			if (nextBatch != null)
				PartiaLeku = nextBatch;
			else
			{
				ErrorMessage = "Brak dostępnej, ważnej partii wybranego wariantu.";
				return;
			}
		}

		ErrorMessage = null;
		PartiaLeku.IloscDostepna--;

		var name = FullProductName();

		var shoppingItem = ShoppingItems.FirstOrDefault(x => x.Name == name);

		if (shoppingItem == null)
		{
			shoppingItem = new PozycjaSprzedazyViewModel
				{ Name = name, Quantity = 0, Lek = SelectedItem!.Lek, Wariant = WariantLeku!, Partia = PartiaLeku };
			_allShoppingItems.Add(shoppingItem);
			ShoppingItems.Add(shoppingItem);
		}

		shoppingItem.Quantity++;
	}

	[RelayCommand]
	public void RemoveItem()
	{
		if (PartiaLeku is null) return;
		var name = FullProductName();
		var shoppingItem = ShoppingItems.FirstOrDefault(x => x.Name == name);
		if (shoppingItem is null) return;
		PartiaLeku.IloscDostepna++;

		shoppingItem.Quantity--;

		if (shoppingItem.Quantity > 0) return;
		_allShoppingItems.Remove(shoppingItem);
		ShoppingItems.Remove(shoppingItem);
	}

	[RelayCommand]
	public void MakeSale(string saleType)
	{
		if (ShoppingItems.Count <= 0) return;
		ErrorMessage = null;

		try
		{
			_saleRepository.BeginTransaction();
			if (saleType == "faktura")
				_saleRepository.Faktura();
			else _saleRepository.Paragon();

			foreach (var pozycja in ShoppingItems) _saleRepository.Add(pozycja);

			var id = _saleRepository.Finish(_recepta?.Id);
			_recepta?.IdSprzedazy = id;
			_recepta?.DataRealizacji = DateTime.Now;
			Exit();
		}
		catch (Exception ex)
		{
			ErrorMessage = ex.Message;
			ShoppingItems.Clear();
			_allShoppingItems.Clear();
			LoadData();
		}
	}

	private void Exit()
	{
		BackRequested?.Invoke();
	}

	[RelayCommand]
	public void FindAlternatives()
	{
		if (SelectedItem is null) return;
		_drugRepository.GetAlternatives(SelectedItem.Lek.SubstancjaCzynna);
	}

	[RelayCommand]
	public void CloseDialog()
	{
	}
}
