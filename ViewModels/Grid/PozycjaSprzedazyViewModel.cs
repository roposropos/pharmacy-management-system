using System;
using Apteka.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apteka.ViewModels.Grid;

public partial class PozycjaSprzedazyViewModel : ObservableObject, IFilterable
{
	[ObservableProperty] [NotifyPropertyChangedFor(nameof(TotalPrice))]
	private decimal _price;

	[ObservableProperty] [NotifyPropertyChangedFor(nameof(TotalPrice))]
	private int _quantity;

	public Lek Lek { get; init; } = null!;
	public PartiaLeku Partia { get; init; } = null!;
	public WariantLeku Wariant { get; init; } = null!;
	public string Name { get; set; } = string.Empty;
	public decimal TotalPrice => Price * Quantity;

	public bool IsMatch(string filter)
	{
		return Name.Contains(filter, StringComparison.OrdinalIgnoreCase);
	}
}
