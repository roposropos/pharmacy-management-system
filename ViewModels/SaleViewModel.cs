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
		var data = _recepta is null
			? _drugRepository.GetNoPrescription()
			: _drugRepository.GetFromPrescription(_recepta.Id);
		foreach (var lek in data) Items.Add(new LekiViewModel(lek));
	}

	private void OnVariantChanged(WariantLeku wariant)
	{
		PartiaLeku = wariant.PartieLekow.First();
	}

	private string FullProductName()
	{
		return $"{SelectedItem?.Nazwa} {WariantLeku?.Dawka}x{WariantLeku?.Ilosc} - {PartiaLeku?.NumerPartii}";
	}

	[RelayCommand]
	public void AddItem()
	{
		if (PartiaLeku is null) return;
		if (PartiaLeku.IloscLaczna <= 0)
		{
			var nextBatch = WariantLeku?.PartieLekow
				.Where(p => p.Id != PartiaLeku.Id && p.IloscDostepna > 0)
				.OrderBy(p => p.DataWaznosci)
				.FirstOrDefault();

			if (nextBatch != null)
				PartiaLeku = nextBatch;
			else
				return;
		}

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
		_saleRepository.BeginTransaction();
		if (saleType == "fakutra")
			_saleRepository.Faktura();
		else _saleRepository.Paragon();

		foreach (var pozycja in ShoppingItems) _saleRepository.Add(pozycja);

		var id = _saleRepository.Finish();
		_recepta?.IdSprzedazy = id;
		_recepta?.DataRealizacji = DateTime.Now;
		Exit();
	}

	private void Exit()
	{
		BackRequested?.Invoke();
	}

	protected override void Search()
	{
		if (string.IsNullOrWhiteSpace(SearchText)) return;
		var filtered = _allShoppingItems.Where(item => item.IsMatch(SearchText)).ToList();
		ShoppingItems.Clear();
		foreach (var item in filtered) ShoppingItems.Add(item);
		Console.WriteLine($"{SearchText}");
	}

	protected override void ClearSearch()
	{
		SearchText = string.Empty;
		ShoppingItems.Clear();
		foreach (var item in _allShoppingItems) ShoppingItems.Add(item);
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