using System;

namespace Apteka.Models;

public class StanPartiiLeku
{
	public int IdPartii { get; set; }
	public int IdWariantu { get; set; }
	public int IdLeku { get; set; }
	public int IdProducenta { get; set; }
	public string NazwaLeku { get; set; } = string.Empty;
	public string NazwaProducenta { get; set; } = string.Empty;
	public string SubstancjaCzynna { get; set; } = string.Empty;
	public long KodEan { get; set; }
	public string Dawka { get; set; } = string.Empty;
	public int IloscWOpakowaniu { get; set; }
	public PostacLeku Postac { get; set; }
	public string NumerPartii { get; set; } = string.Empty;
	public DateTime DataWaznosci { get; set; }
	public int IloscDostepna { get; set; }
	public int IloscZarezerwowana { get; set; }
	public int IloscDoSprzedazy => IloscDostepna - IloscZarezerwowana;
	public string PelnaNazwa => $"{NazwaLeku} {Dawka} x{IloscWOpakowaniu}";
}
