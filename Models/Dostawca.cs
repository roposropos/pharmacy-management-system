namespace Apteka.Models;

public class Dostawca
{
	public int Id { get; set; }
	public string Nazwa { get; set; } = string.Empty;
	public string NIP { get; set; } = string.Empty;
	public int IdAdresu { get; set; }
	public virtual Adres? Adres { get; set; }
}