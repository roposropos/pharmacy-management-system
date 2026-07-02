using System;

namespace Apteka.Models;

public class Surowiec
{
	public int Id { get; set; }
	public string Nazwa { get; set; } = string.Empty;
	public string Typ { get; set; } = "pomocniczy";
	public string Jednostka { get; set; } = "g";
	public decimal DostepnaIlosc { get; set; }
	public DateTime? NajblizszaDataWaznosci { get; set; }
}
