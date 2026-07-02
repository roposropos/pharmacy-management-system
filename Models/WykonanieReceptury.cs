using System;

namespace Apteka.Models;

public class WykonanieReceptury
{
	public int Id { get; set; }
	public int IdReceptury { get; set; }
	public string NazwaReceptury { get; set; } = string.Empty;
	public int? IdRecepty { get; set; }
	public int IdSprzedazy { get; set; }
	public DateTime DataWykonania { get; set; }
	public int Ilosc { get; set; }
	public decimal KosztJednostkowy { get; set; }
}
