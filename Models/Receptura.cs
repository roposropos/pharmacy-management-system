using System.Collections.Generic;

namespace Apteka.Models;

public class Receptura
{
	public int Id { get; set; }
	public string Nazwa { get; set; } = string.Empty;
	public string Opis { get; set; } = string.Empty;
	public bool Zatwierdzona { get; set; }
	public decimal KosztPrzygotowania { get; set; }
	public List<RecepturaSkladnik> Skladniki { get; set; } = new();
}
