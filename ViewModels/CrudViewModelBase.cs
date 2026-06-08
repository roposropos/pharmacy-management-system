using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Apteka.Models;
using Apteka.ViewModels.Grid;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Apteka.ViewModels;

public interface ICrudActions
{
	ICommand AddCommand { get; }
	ICommand DeleteCommand { get; }
	ICommand EditCommand { get; }
}

public interface ISearchableViewModel
{
	string SearchText { get; set; }
	ICommand SearchCommand { get; }
	ICommand ClearSearchCommand { get; }
}

public abstract partial class CrudViewModelBase<T> : ViewModelBase, ICrudActions, ISearchableViewModel
	where T : class, IHasId, IFilterable, new()
{
	protected List<T> _allItems = new();

	[ObservableProperty] [NotifyCanExecuteChangedFor(nameof(AddCommand))]
	private bool _canAdd = true;

	[ObservableProperty] [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
	private bool _canDelete = true;

	[ObservableProperty] [NotifyCanExecuteChangedFor(nameof(EditCommand))]
	private bool _canEdit = true;

	[ObservableProperty] private bool _isEditing;

	[ObservableProperty] private string _searchText = string.Empty;

	[ObservableProperty] private T? _selectedItem;
	public ObservableCollection<T> Items { get; } = new();

	public List<int> DeletedIds { get; private set; } = [];
	public bool HasDataToDelete => DeletedIds.Count > 0;
	ICommand ICrudActions.AddCommand => AddCommand;
	ICommand ICrudActions.EditCommand => EditCommand;
	ICommand ICrudActions.DeleteCommand => DeleteCommand;

	ICommand ISearchableViewModel.SearchCommand => SearchCommand;

	ICommand ISearchableViewModel.ClearSearchCommand => ClearSearchCommand;

	[RelayCommand]
	protected virtual void Search()
	{
		if (string.IsNullOrWhiteSpace(SearchText)) return;
		var filtered = _allItems.Where(item => item.IsMatch(SearchText)).ToList();
		Items.Clear();
		foreach (var item in filtered) Items.Add(item);
		Console.WriteLine($"{SearchText}");
	}

	[RelayCommand]
	protected virtual void ClearSearch()
	{
		SearchText = string.Empty;
		Items.Clear();
		foreach (var item in _allItems)
			Items.Add(item);
	}

	[RelayCommand(CanExecute = nameof(CanAdd))]
	protected virtual void Add()
	{
		if (!CanAdd) return;
		var item = new T();
		Items.Add(item);
		SelectedItem = item;
		IsEditing = true;
	}

	[RelayCommand(CanExecute = nameof(CanDelete))]
	protected virtual void Delete()
	{
		if (!CanDelete) return;
		if (SelectedItem == null) return;
		DeletedIds.Add(SelectedItem.Id);
		Items.Remove(SelectedItem);
		SelectedItem = null;
	}

	[RelayCommand(CanExecute = nameof(CanEdit))]
	protected virtual void Edit()
	{
		if (SelectedItem is null) return;
		IsEditing = true;
	}

	[RelayCommand]
	protected virtual void Cancel()
	{
		IsEditing = false;
		DeletedIds = [];
		LoadData();
	}

	[RelayCommand]
	protected virtual void Save()
	{
		IsEditing = false;
	}

	protected virtual void LoadData()
	{
		DeletedIds = [];
		Items.Clear();
	}
}