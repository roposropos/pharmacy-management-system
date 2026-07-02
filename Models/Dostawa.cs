using System;

namespace Apteka.Models;

public class Dostawa
{
	public int Id { get; set; }
	public DateTime DataDostawy { get; set; }
	public int IdDostawcy { get; set; }
	public virtual Dostawca? Dostawca { get; set; }
}