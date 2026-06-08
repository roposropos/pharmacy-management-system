using System;
using System.Collections.Generic;
using System.Linq;
using Apteka.Models;
using Apteka.ViewModels.Grid;
using CommunityToolkit.Mvvm.Input;

namespace Apteka.ViewModels;

public partial class PhonesViewModel : CrudViewModelBase<TelefonViewModel>
{
	private readonly List<Telefon> _numery;
	private readonly List<Telefon> _originalNumery;

	public PhonesViewModel(Uzytkownik uzytkownik, List<Telefon> numery)
	{
		_originalNumery = numery;
		_numery = new List<Telefon>(numery);
		CanAdd = true;
		CanDelete = uzytkownik.Rola == "kierownik";
		CanEdit = uzytkownik.Rola == "kierownik";
		LoadData();
	}

	private bool HasUnsavedChanges => Items.Any(x => x.IsModified);
	private bool HasErrors => Items.Any(x => x.HasErrors);
	public event Action? BackRequested;

	[RelayCommand]
	protected sealed override void LoadData()
	{
		base.LoadData();
		_allItems.Clear();
		foreach (var numer in _numery)
		{
			var item = new TelefonViewModel(numer);
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

	protected sealed override void Cancel()
	{
		base.Cancel();
	}

	protected sealed override void Save()
	{
		if (HasErrors) return;
		base.Save();
		if (HasDataToDelete)
		{
			foreach (var id in DeletedIds) _numery.RemoveAll(x => x.Id == id);

			LoadData();
			return;
		}

		if (!HasUnsavedChanges) return;
		Console.WriteLine("Saving changes");
		foreach (var modified in Items.Where(x => x.IsModified))
		{
			if (modified.Id == 0)
			{
				_numery.Add(modified.Telefon);
				continue;
			}

			var phone = _numery.FirstOrDefault(x => x.Id == modified.Id);
			if (phone is null) continue;
			phone.Numer = modified.Numer;
			phone.Opis = string.IsNullOrWhiteSpace(modified.Opis) ? null : modified.Opis;
		}

		_originalNumery.Clear();
		_originalNumery.AddRange(_numery);
		LoadData();
	}
}