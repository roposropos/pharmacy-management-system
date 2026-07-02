using System;

namespace Apteka.Models;

public class Sprzedarz
{
	public int Id { get; set; }
	public DateTime Data { get; set; }
	public decimal KwotaBrutto { get; set; }
}

public class StanMagazynu
{
	public string PelnaNazwa { get; set; } = string.Empty;
	public int DostepnaIlosc { get; set; }
	public DateTime? DataWaznosci { get; set; }
	public Typ Typ { get; set; }
}

public enum Typ
{
	Lek,
	Surowiec
}