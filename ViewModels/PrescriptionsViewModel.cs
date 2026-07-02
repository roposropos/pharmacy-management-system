using System;
using System.Collections.ObjectModel;
using System.Linq;
using Apteka.Models;
using Apteka.Repositories;
using Apteka.Services;
using Apteka.ViewModels.Grid;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Apteka.ViewModels;

public partial class PrescriptionsViewModel : CrudViewModelBase<ReceptaViewModel>
{
	private readonly CompoundingRepository _compoundingRepository;
	private readonly DrugRepository _drugRepository;
	private readonly PrescriptionRepository _prescriptionRepository;
	private readonly Uzytkownik _uzytkownik;

	[ObservableProperty] private ObservableCollection<WykonanieRecepturyViewModel> _compoundExecutions = new();
	[ObservableProperty] private string _compoundMessage = string.Empty;
	[ObservableProperty] private string _documentType = "Paragon";
	[ObservableProperty] private decimal _ingredientAmount = 1;
	[ObservableProperty] private DateTimeOffset _newBatchExpiry = DateTimeOffset.Now.AddYears(2);
	[ObservableProperty] private string _newBatchNumber = string.Empty;
	[ObservableProperty] private decimal _newBatchQuantity;
	[ObservableProperty] private ObservableCollection<PartiaSurowcaViewModel> _rawBatches = new();
	[ObservableProperty] private ObservableCollection<SurowiecViewModel> _rawMaterials = new();
	[ObservableProperty] private string _rawMaterialMessage = string.Empty;
	[ObservableProperty] private ObservableCollection<RecepturaViewModel> _recipes = new();
	[ObservableProperty] private int _recipeExecutionAmount = 1;
	[ObservableProperty] private string _recipeMessage = string.Empty;
	[ObservableProperty] private PartiaSurowcaViewModel? _selectedRawBatch;
	[ObservableProperty] private SurowiecViewModel? _selectedRawMaterial;
	[ObservableProperty] private RecepturaViewModel? _selectedRecipe;
	[ObservableProperty] private RecepturaSkladnikViewModel? _selectedRecipeIngredient;

	public PrescriptionsViewModel(DrugRepository drugRepository, Uzytkownik uzytkownik)
	{
		_drugRepository = drugRepository;
		var dbService = App.Current.Services!.GetRequiredService<DatabaseService>();
		var sensitiveDataProtector = App.Current.Services!.GetRequiredService<SensitiveDataProtector>();
		_prescriptionRepository = new PrescriptionRepository(dbService, sensitiveDataProtector);
		_compoundingRepository = new CompoundingRepository(dbService);
		_uzytkownik = uzytkownik;
		CanAdd = uzytkownik.Rola == "kierownik";
		CanDelete = uzytkownik.Rola == "kierownik";
		CanEdit = true;
		LoadData();
	}

	public string[] DocumentTypes { get; } = ["Paragon", "Faktura"];
	public string[] RawMaterialTypes { get; } = ["czynny", "pomocniczy"];
	public bool CanManageCompounding => _uzytkownik.Rola == "kierownik";
	public bool CanExecuteCompounding => true;

	private bool HasUnsavedChanges => Items.Any(x => x.IsModified)
	                                  || RawMaterials.Any(x => x.IsModified)
	                                  || Recipes.Any(x => x.IsModified);

	public event Action<ViewModelBase>? NavigateRequested;
	public event Action? BackRequested;

	[RelayCommand]
	protected sealed override void LoadData()
	{
		base.LoadData();
		_allItems.Clear();
		foreach (var recepta in _prescriptionRepository.GetAll())
		{
			var item = new ReceptaViewModel(recepta);
			Items.Add(item);
			_allItems.Add(item);
		}

		LoadCompoundingData();
	}

	[RelayCommand]
	public void Realize()
	{
		if (SelectedItem == null) return;
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

	[RelayCommand]
	private void AddRawMaterial()
	{
		if (!CanManageCompounding) return;
		var item = new SurowiecViewModel();
		RawMaterials.Add(item);
		SelectedRawMaterial = item;
		RawMaterialMessage = "Dodano nowy surowiec roboczo.";
	}

	[RelayCommand]
	private void SaveRawMaterials()
	{
		if (!CanManageCompounding) return;

		try
		{
			foreach (var rawMaterial in RawMaterials.Where(x => x.IsModified))
				_compoundingRepository.AddOrUpdateRawMaterial(rawMaterial.Surowiec);

			RawMaterialMessage = "Surowce zostały zapisane.";
			LoadCompoundingData();
		}
		catch (Exception ex)
		{
			RawMaterialMessage = $"Nie zapisano surowców: {ex.Message}";
		}
	}

	[RelayCommand]
	private void AddRawBatch()
	{
		if (!CanManageCompounding || SelectedRawMaterial is null) return;

		try
		{
			_compoundingRepository.AddOrUpdateRawBatch(new PartiaSurowca
			{
				IdSurowca = SelectedRawMaterial.Id,
				NumerPartii = NewBatchNumber,
				DataWaznosci = NewBatchExpiry.DateTime,
				IloscDostepna = NewBatchQuantity
			});
			RawMaterialMessage = "Partia surowca została dodana.";
			NewBatchNumber = string.Empty;
			NewBatchQuantity = 0;
			LoadCompoundingData();
		}
		catch (Exception ex)
		{
			RawMaterialMessage = $"Nie dodano partii: {ex.Message}";
		}
	}

	[RelayCommand]
	private void AddRecipe()
	{
		if (!CanManageCompounding) return;
		var item = new RecepturaViewModel();
		Recipes.Add(item);
		SelectedRecipe = item;
		RecipeMessage = "Dodano nową recepturę roboczo.";
	}

	[RelayCommand]
	private void AddIngredientToRecipe()
	{
		if (!CanManageCompounding || SelectedRecipe is null || SelectedRawMaterial is null) return;
		if (IngredientAmount <= 0)
		{
			RecipeMessage = "Ilość składnika musi być większa od zera.";
			return;
		}

		SelectedRecipe.AddIngredient(SelectedRawMaterial.Surowiec, IngredientAmount);
		RecipeMessage = "Składnik został dodany do receptury.";
	}

	[RelayCommand]
	private void RemoveIngredientFromRecipe()
	{
		if (!CanManageCompounding || SelectedRecipe is null || SelectedRecipeIngredient is null) return;
		SelectedRecipe.Skladniki.Remove(SelectedRecipeIngredient);
		SelectedRecipe.IsModified = true;
		SelectedRecipeIngredient = SelectedRecipe.Skladniki.FirstOrDefault();
		RecipeMessage = "Składnik został usunięty z receptury.";
	}

	[RelayCommand]
	private void SaveRecipe()
	{
		if (!CanManageCompounding || SelectedRecipe is null) return;

		try
		{
			_compoundingRepository.AddOrUpdateRecipe(SelectedRecipe.ToModel());
			RecipeMessage = "Receptura została zapisana.";
			LoadCompoundingData();
		}
		catch (Exception ex)
		{
			RecipeMessage = $"Nie zapisano receptury: {ex.Message}";
		}
	}

	[RelayCommand]
	private void DeleteRecipe()
	{
		if (!CanManageCompounding || SelectedRecipe is null) return;

		try
		{
			if (SelectedRecipe.Id > 0)
				_compoundingRepository.DeleteRecipe(SelectedRecipe.Id);
			Recipes.Remove(SelectedRecipe);
			SelectedRecipe = Recipes.FirstOrDefault();
			RecipeMessage = "Receptura została usunięta.";
		}
		catch (Exception ex)
		{
			RecipeMessage = $"Nie usunięto receptury: {ex.Message}";
		}
	}

	[RelayCommand]
	private void ExecuteRecipe()
	{
		if (!CanExecuteCompounding || SelectedRecipe is null) return;

		try
		{
			var prescriptionId = SelectedItem?.Zrealizowana == true ? null : SelectedItem?.Id;
			var executionId = _compoundingRepository.ExecuteRecipe(
				SelectedRecipe.Id,
				prescriptionId,
				RecipeExecutionAmount,
				DocumentType);

			CompoundMessage = $"Wykonano recepturę. Numer wykonania: {executionId}.";
			LoadData();
		}
		catch (Exception ex)
		{
			CompoundMessage = $"Nie wykonano receptury: {ex.Message}";
		}
	}

	partial void OnSelectedRecipeChanged(RecepturaViewModel? value)
	{
		SelectedRecipeIngredient = value?.Skladniki.FirstOrDefault();
	}

	private void LoadCompoundingData()
	{
		RawMaterials = new ObservableCollection<SurowiecViewModel>(
			_compoundingRepository.GetRawMaterials().Select(x => new SurowiecViewModel(x)));
		RawBatches = new ObservableCollection<PartiaSurowcaViewModel>(
			_compoundingRepository.GetRawMaterialBatches().Select(x => new PartiaSurowcaViewModel(x)));
		Recipes = new ObservableCollection<RecepturaViewModel>(
			_compoundingRepository.GetRecipes().Select(x => new RecepturaViewModel(x)));
		CompoundExecutions = new ObservableCollection<WykonanieRecepturyViewModel>(
			_compoundingRepository.GetExecutions().Select(x => new WykonanieRecepturyViewModel(x)));

		SelectedRawMaterial = RawMaterials.FirstOrDefault();
		SelectedRawBatch = RawBatches.FirstOrDefault();
		SelectedRecipe = Recipes.FirstOrDefault();
	}
}
