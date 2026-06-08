using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apteka.Models;

public partial class PartiaLeku : ObservableObject
{
	[ObservableProperty] [NotifyPropertyChangedFor(nameof(IloscLaczna))]
	private int _iloscDostepna;

	[ObservableProperty] [NotifyPropertyChangedFor(nameof(IloscLaczna))]
	private int _iloscZarezerwowana;

	public int Id { get; init; }

	public string NumerPartii { get; set; } = string.Empty;

	public DateTime DataWaznosci { get; set; }
	public int IloscLaczna => IloscDostepna - IloscZarezerwowana;
}