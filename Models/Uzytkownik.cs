namespace Apteka.Models;

public class Uzytkownik
{
	public int Id { get; set; }
	public string Login { get; set; } = string.Empty;
	public string Rola { get; init; } = string.Empty;
	public int IdOsoby { get; set; }
	public virtual Osoba Osoba { get; init; } = new();
	public string FullName => $"{Osoba.Imie} {Osoba.Nazwisko}";
}