using System;
using System.Linq;
using Apteka.Models;
using Apteka.Repositories;
using Apteka.ViewModels.Grid;
using CommunityToolkit.Mvvm.Input;

namespace Apteka.ViewModels;

public partial class DrugsViewModel : CrudViewModelBase<LekiViewModel>
{
	private readonly DrugRepository _drugRepository;

	public DrugsViewModel(DrugRepository drugRepository, Uzytkownik uzytkownik)
	{
		_drugRepository = drugRepository;

		LoadData();
	}

	private bool HasUnsavedChanges => Items.Any(x => x.IsModified);
	public event Action? BackRequested;

	[RelayCommand]
	protected sealed override void LoadData()
	{
		base.LoadData();
		_allItems.Clear();
		var data = _drugRepository.GetAll();
		foreach (var lek in data)
		{
			var item = new LekiViewModel(lek);
			Items.Add(item);
			_allItems.Add(item);
		}
	}

	[RelayCommand]
	public void GoBack()
	{
		if (HasUnsavedChanges) return;
		BackRequested?.Invoke();
	}
}