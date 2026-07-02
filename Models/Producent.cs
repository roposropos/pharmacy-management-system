namespace Apteka.Models;

public class Producent
{
	public int Id { get; init; }
	public string Nazwa { get; set; } = string.Empty;
	public int IdAdresu { get; set; }
	public virtual Adres Adres { get; set; } = new();
}