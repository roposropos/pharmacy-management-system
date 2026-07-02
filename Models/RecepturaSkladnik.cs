namespace Apteka.Models;

public class RecepturaSkladnik
{
	public int IdReceptury { get; set; }
	public int IdSurowca { get; set; }
	public string NazwaSurowca { get; set; } = string.Empty;
	public string Jednostka { get; set; } = "g";
	public decimal Ilosc { get; set; }
}
