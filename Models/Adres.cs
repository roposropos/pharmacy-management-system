namespace Apteka.Models;

public class Adres
{
	public int Id { get; init; }
	public string Kraj { get; set; } = string.Empty;
	public string? Ulica { get; set; }
	public string NumerDomu { get; set; } = string.Empty;
	public string? NumerLokalu { get; set; }
	public string Miejscowosc { get; set; } = string.Empty;
	public string KodPocztowy { get; set; } = string.Empty;

	public string PelnyAdres
	{
		get
		{
			if (string.IsNullOrWhiteSpace(Kraj)) return string.Empty;
			var adres = Kraj;

			if (!string.IsNullOrWhiteSpace(Ulica)) adres += ", " + Ulica;

			if (string.IsNullOrWhiteSpace(NumerDomu)) return string.Empty;
			adres += ", " + NumerDomu;

			if (!string.IsNullOrWhiteSpace(NumerLokalu)) adres += "/" + NumerLokalu;

			if (string.IsNullOrWhiteSpace(Miejscowosc)) return string.Empty;
			adres += ", " + Miejscowosc;

			if (string.IsNullOrWhiteSpace(KodPocztowy)) return string.Empty;
			adres += ", " + KodPocztowy;

			return adres;
		}
	}

	public bool IsEmpty => string.IsNullOrWhiteSpace(PelnyAdres);

	public override string ToString()
	{
		return PelnyAdres;
	}
}