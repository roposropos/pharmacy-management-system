using System.Collections.Generic;

namespace Apteka.Models;

public class Lek
{
	public int Id { get; init; }

	public string Nazwa { get; set; } = string.Empty;

	public int IdProducenta { get; set; }
	public virtual Producent Producent { get; set; } = new();

	public string SubstancjaCzynna { get; set; } = string.Empty;
	public bool BezRecepty { get; set; }

	public virtual ICollection<WariantLeku> Warianty { get; set; } = new List<WariantLeku>();
}