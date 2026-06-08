using System;
using Apteka.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apteka.ViewModels.Grid;

public partial class ReceptaViewModel : EditableViewModelBase, IFilterable
{
	[ObservableProperty] private Recepta _recepta;

	public ReceptaViewModel()
	{
		_recepta = new Recepta();
		IsModified = true;
	}

	public ReceptaViewModel(Recepta recepta)
	{
		_recepta = recepta;
		IsModified = false;
	}

	public override int Id => Recepta.Id;

	public ushort Kod => Recepta.Kod;
	public DateTime DataWystawienia => Recepta.DataWystawienia;

	public DateTime? DataRealizacji
	{
		get => Recepta.DataRealizacji;
		set
		{
			if (Recepta.DataRealizacji == value) return;
			Recepta.DataRealizacji = value;
			OnPropertyChanged();
			IsModified = true;
		}
	}

	public DateTime DataWaznosci => Recepta.DataWaznosci;

	public int? PoprzedniaReceptaId => Recepta.ReceptaNadrzedna?.Id;

	public bool IsMatch(string searchText)
	{
		return Kod.ToString().Contains(searchText);
	}
}