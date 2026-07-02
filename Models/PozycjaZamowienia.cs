namespace Apteka.Models;

	public class PozycjaZamowienia
	{
		public int Id { get; set; }
		public int IdZamowienia { get; set; }
		public int? IdWariantu { get; set; }
		public int? IdSurowca { get; set; }
		public string TypProduktu { get; set; } = "Lek";
		public string Nazwa { get; set; } = string.Empty;
		public decimal Ilosc { get; set; }
		public decimal CenaSzacowana { get; set; }
	}
