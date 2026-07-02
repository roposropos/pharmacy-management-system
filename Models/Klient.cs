namespace Apteka.Models;

public class Klient
{
	public int Id { get; init; }
	public string Pesel { get; set; } = string.Empty;

	public int IdOsoby { get; set; }
	public virtual Osoba Osoba { get; set; } = new();

	public int IdAdresu { get; set; }
	public virtual Adres Adres { get; set; } = new();
}