using System;

namespace Apteka.Models;

	public class PozycjaDostawy
	{
		public int Id { get; set; }
		public int IdDostawy { get; set; }
		public int? IdWariantu { get; set; }
		public int? IdSurowca { get; set; }
		public int? IdPartii { get; set; }
		public int? IdPartiiSurowca { get; set; }
		public string TypProduktu { get; set; } = "Lek";
		public string Nazwa { get; set; } = string.Empty;
		public string NumerPartii { get; set; } = string.Empty;
		public DateTime DataWaznosci { get; set; }
		public decimal Ilosc { get; set; }
		public decimal CenaZakupu { get; set; }
	}
