using System;
using System.Collections.Generic;
using Apteka.Models;

namespace Apteka.Repositories;

public class PrescriptionRepository(DatabaseService dbService)
{
	public IEnumerable<Recepta> GetAll()
	{
		var lookup = new Dictionary<int, Recepta>();
		using var connection = dbService.CreateConnection();
		using (var command = connection.CreateCommand())
		{
			command.CommandText = """
			                      SELECT
			                          r.id_recepty, r.data_wystawienia, r.data_realizacji, r.data_waznosci, r.kod,
			                          l.id_lekarza, l."numer_PWZ", lo.id_osoby, lo.imie as imie_lekarza, lo.nazwisko as nazwisko_lekarza,
			                          k.id_klienta, k.pesel
			                      FROM apteka.recepta r
			                               JOIN apteka.lekarze l ON l.id_lekarza = r.id_lekarza_lekarze
			                               JOIN apteka.osoby lo ON lo.id_osoby = l.id_osoby_osoby
			                               LEFT JOIN apteka.klienci k ON k.id_klienta = r.id_klienta_klienci
			                      """;
			using (var reader = command.ExecuteReader())
			{
				while (reader.Read())
				{
					if (lookup.TryGetValue(reader.GetInt32(0), out var currentRecepta)) continue;
					currentRecepta = new Recepta
					{
						Id = reader.GetInt32(0),
						DataWystawienia = Convert.ToDateTime(reader["data_wystawienia"]),
						DataRealizacji = reader.IsDBNull(2)
							? null
							: Convert
								.ToDateTime(reader["data_realizacji"]),
						DataWaznosci = Convert.ToDateTime(reader["data_waznosci"]),
						Kod = Convert.ToUInt16(reader["kod"]),
						IdLekarza = Convert.ToInt32(reader["id_lekarza"]),
						Lekarz = new Lekarz
						{
							Id = Convert.ToInt32(reader["id_lekarza"]),
							NumerPwz = Convert.ToInt32(reader["numer_PWZ"]),
							IdOsoby = Convert.ToInt32(reader["id_osoby"]),
							Osoba = new Osoba
							{
								Id = Convert.ToInt32(reader["id_osoby"]),
								Imie = reader["imie_lekarza"].ToString() ?? string.Empty,
								Nazwisko = reader["nazwisko_lekarza"].ToString() ?? string.Empty
							}
						},
						//10
						IdKlienta = null,
						//11
						//12
						IdRecepty = null,
						IdSprzedazy = null
					};
					lookup.Add(currentRecepta.Id, currentRecepta);
				}
			}
		}

		return lookup.Values;
	}
}