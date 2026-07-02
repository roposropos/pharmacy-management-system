using System;
using Apteka.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apteka.ViewModels.Grid;

	public partial class DeliveryLineViewModel : ObservableObject
	{
		public int? IdWariantu { get; init; }
		public int? IdSurowca { get; init; }
		public string TypProduktu { get; init; } = "Lek";
		public string Nazwa { get; init; } = string.Empty;
		public string NumerPartii { get; init; } = string.Empty;
		public DateTime DataWaznosci { get; init; }

		[ObservableProperty] private decimal _ilosc;
		[ObservableProperty] private decimal _cenaZakupu;

	public PozycjaDostawy ToModel()
	{
			return new PozycjaDostawy
			{
				IdWariantu = IdWariantu,
				IdSurowca = IdSurowca,
				TypProduktu = TypProduktu,
				Nazwa = Nazwa,
				NumerPartii = NumerPartii,
			DataWaznosci = DataWaznosci,
			Ilosc = Ilosc,
			CenaZakupu = CenaZakupu
		};
	}
}
