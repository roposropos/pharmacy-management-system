using Apteka.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Apteka.ViewModels.Grid;

	public partial class OrderLineViewModel : ObservableObject
	{
		public int? IdWariantu { get; init; }
		public int? IdSurowca { get; init; }
		public string TypProduktu { get; init; } = "Lek";
		public string Nazwa { get; init; } = string.Empty;

		[ObservableProperty] private decimal _ilosc;
		[ObservableProperty] private decimal _cenaSzacowana;

	public PozycjaZamowienia ToModel()
	{
			return new PozycjaZamowienia
			{
				IdWariantu = IdWariantu,
				IdSurowca = IdSurowca,
				TypProduktu = TypProduktu,
				Nazwa = Nazwa,
				Ilosc = Ilosc,
				CenaSzacowana = CenaSzacowana
		};
	}
}
