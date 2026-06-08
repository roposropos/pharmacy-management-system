using System.Collections.Generic;

namespace Apteka.Models;

public class Osoba
{
	public int Id { get; init; }
	public string Imie { get; set; } = string.Empty;
	public string Nazwisko { get; set; } = string.Empty;

	public virtual ICollection<Telefon> Telefony { get; set; } = new List<Telefon>();
}