using System;

namespace Apteka.Models;

public class Uzytkownik
{
	public int Id { get; set; }
	public string Login { get; set; } = string.Empty;
	public string Rola { get; set; } = string.Empty;
	public bool Aktywny { get; set; } = true;
	public DateTime? OstatnieLogowanie { get; set; }
	public int IdOsoby { get; set; }
	public virtual Osoba Osoba { get; set; } = new();
	public string FullName => $"{Osoba.Imie} {Osoba.Nazwisko}";
}
