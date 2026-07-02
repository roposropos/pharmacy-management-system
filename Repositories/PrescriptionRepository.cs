using System;
using System.Collections.Generic;
using Apteka.Models;
using Apteka.Services;

namespace Apteka.Repositories;

public class PrescriptionRepository(DatabaseService dbService, SensitiveDataProtector sensitiveDataProtector)
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
			                          r.id_recepty_recepta, r.id_sprzedazy_sprzedaze,
			                          l.id_lekarza, l."numer_PWZ", lo.id_osoby, lo.imie as imie_lekarza, lo.nazwisko as nazwisko_lekarza,
			                          k.id_klienta, k.pesel, ko.id_osoby AS id_osoby_klienta,
			                          ko.imie AS imie_klienta, ko.nazwisko AS nazwisko_klienta
			                      FROM apteka.recepta r
			                               JOIN apteka.lekarze l ON l.id_lekarza = r.id_lekarza_lekarze
			                               JOIN apteka.osoby lo ON lo.id_osoby = l.id_osoby_osoby
			                               LEFT JOIN apteka.klienci k ON k.id_klienta = r.id_klienta_klienci
			                               LEFT JOIN apteka.osoby ko ON ko.id_osoby = k.id_osoby_osoby
			                      ORDER BY r.data_wystawienia DESC, r.id_recepty DESC
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
						IdKlienta = reader["id_klienta"] == DBNull.Value ? null : Convert.ToInt32(reader["id_klienta"]),
						Klient = reader["id_klienta"] == DBNull.Value
							? null
							: new Klient
							{
								Id = Convert.ToInt32(reader["id_klienta"]),
								Pesel = sensitiveDataProtector.Unprotect(reader["pesel"].ToString()),
								IdOsoby = Convert.ToInt32(reader["id_osoby_klienta"]),
								Osoba = new Osoba
								{
									Id = Convert.ToInt32(reader["id_osoby_klienta"]),
									Imie = reader["imie_klienta"].ToString() ?? string.Empty,
									Nazwisko = reader["nazwisko_klienta"].ToString() ?? string.Empty
								}
							},
						IdRecepty = reader["id_recepty_recepta"] == DBNull.Value
							? null
							: Convert.ToInt32(reader["id_recepty_recepta"]),
						IdSprzedazy = reader["id_sprzedazy_sprzedaze"] == DBNull.Value
							? null
							: Convert.ToInt32(reader["id_sprzedazy_sprzedaze"])
					};
					lookup.Add(currentRecepta.Id, currentRecepta);
				}
			}
		}

		return lookup.Values;
	}
}
