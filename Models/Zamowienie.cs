using System;

namespace Apteka.Models;

public class Zamowienie
{
	public int Id { get; set; }
	public DateTime DataUtworzenia { get; set; }
	public string Status { get; set; } = "Nowe";
	public string Typ { get; set; } = string.Empty;
	public int IdDostawcy { get; set; }
	public virtual Dostawca? Dostawca { get; set; }
}