using System;
using Apteka.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apteka.ViewModels.Grid;

public partial class TelefonViewModel : EditableViewModelBase, IFilterable
{
	[ObservableProperty] private Telefon _telefon;

	public TelefonViewModel()
	{
		_telefon = new Telefon();
		IsModified = true;
	}

	public TelefonViewModel(Telefon telefon)
	{
		_telefon = telefon;
		IsModified = false;
	}

	public override int Id => Telefon.Id;

	public string Opis
	{
		get => Telefon.Opis ?? string.Empty;
		set
		{
			if (Telefon.Opis == value) return;
			Telefon.Opis = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public string Numer
	{
		get => Telefon.Numer;
		set
		{
			if (Telefon.Numer == value) return;
			Telefon.Numer = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public bool IsMatch(string searchText)
	{
		return Numer.Contains(searchText)
		       || Opis.Contains(searchText, StringComparison.OrdinalIgnoreCase);
	}
}