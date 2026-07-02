using System.Collections.Generic;
using System.Linq;

namespace Apteka.Models;

public class WariantLeku
{
	public int Id { get; set; }
	public long KodEan { get; set; }
	public string Dawka { get; set; } = string.Empty;
	public int Ilosc { get; set; }
	public PostacLeku Postac { get; set; }

	public int DostepnaIlosc => PartieLekow.Sum(pl => pl.IloscDostepna - pl.IloscZarezerwowana);
	public virtual ICollection<PartiaLeku> PartieLekow { get; set; } = new List<PartiaLeku>();
}