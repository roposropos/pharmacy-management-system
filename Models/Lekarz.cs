namespace Apteka.Models;

public class Lekarz
{
	public int Id { get; init; }
	public int NumerPwz { get; set; }

	public int IdOsoby { get; set; }
	public virtual Osoba Osoba { get; set; } = new();
}