using System;

namespace Apteka.Models;

public class PartiaSurowca
{
	public int Id { get; set; }
	public int IdSurowca { get; set; }
	public string NazwaSurowca { get; set; } = string.Empty;
	public string Jednostka { get; set; } = "g";
	public string NumerPartii { get; set; } = string.Empty;
	public DateTime DataWaznosci { get; set; }
	public decimal IloscDostepna { get; set; }
	public decimal IloscZarezerwowana { get; set; }
	public decimal IloscDoUzycia => IloscDostepna - IloscZarezerwowana;
}
