namespace Apteka.Models;

public class Telefon
{
	public int Id { get; init; }
	public string Numer { get; set; } = string.Empty;
	public string? Opis { get; set; }
}