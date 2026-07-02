using System;
using System.Collections.Generic;

namespace Apteka.Models;

public class Recepta
{
	public int Id { get; init; }
	public DateTime DataWystawienia { get; set; }
	public DateTime? DataRealizacji { get; set; }
	public DateTime DataWaznosci { get; set; }
	public ushort Kod { get; set; }

	public int? IdSprzedazy { get; set; }

	public int? IdKlienta { get; set; }
	public virtual Klient? Klient { get; set; }

	public int IdLekarza { get; set; }
	public virtual Lekarz? Lekarz { get; set; }

	public int? IdRecepty { get; set; }
	public Recepta? ReceptaNadrzedna { get; set; }

	public List<WariantLeku> WariantyLekow { get; set; } = new();
}