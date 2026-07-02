using Apteka.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apteka.ViewModels.Grid;

public partial class RecepturaSkladnikViewModel : ObservableObject
{
	[ObservableProperty] private RecepturaSkladnik _skladnik;

	public RecepturaSkladnikViewModel()
	{
		_skladnik = new RecepturaSkladnik();
	}

	public RecepturaSkladnikViewModel(RecepturaSkladnik skladnik)
	{
		_skladnik = skladnik;
	}

	public int IdSurowca => Skladnik.IdSurowca;
	public string NazwaSurowca => Skladnik.NazwaSurowca;
	public string Jednostka => Skladnik.Jednostka;

	public decimal Ilosc
	{
		get => Skladnik.Ilosc;
		set
		{
			if (Skladnik.Ilosc == value) return;
			Skladnik.Ilosc = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(Opis));
		}
	}

	public string Opis => $"{NazwaSurowca}: {Ilosc:0.###} {Jednostka}";

	public RecepturaSkladnik ToModel()
	{
		return Skladnik;
	}
}
