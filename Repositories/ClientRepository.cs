using System;
using System.Collections.Generic;
using System.Data.Odbc;
using Apteka.Models;

namespace Apteka.Repositories;

public class ClientRepository(
	DatabaseService dbService,
	AddressRepository addressRepository,
	PhoneRepository phoneRepository)
{
	public IEnumerable<Klient> GetAll()
	{
		var lookup = new Dictionary<int, Klient>();

		using var connection = dbService.CreateConnection();

		using (var command = connection.CreateCommand())
		{
			command.CommandText = """
			                            SELECT 
			                                k.id_klienta, k.pesel, k.id_adresu_adresy AS id_adresu,
			                                o.id_osoby, o.imie, o.nazwisko,
			                                nt.id_telefonu, nt.numer, nt.opis
			                            FROM apteka.klienci k
			                            JOIN apteka.osoby o ON k.id_osoby_osoby = o.id_osoby
			                            LEFT JOIN apteka.pozycja_numeru pn ON o.id_osoby = pn.id_osoby_osoby
			                            LEFT JOIN apteka.numery_telefonu nt ON pn.id_telefonu_numery_telefonu = nt.id_telefonu
			                      """;

			using (var reader = command.ExecuteReader())
			{
				while (reader.Read())
				{
					var idKlienta = Convert.ToInt32(reader["id_klienta"]);

					if (!lookup.TryGetValue(idKlienta, out var currentClient))
					{
						currentClient = new Klient
						{
							Id = idKlienta,
							Pesel = reader["pesel"].ToString() ?? string.Empty,
							IdOsoby = Convert.ToInt32(reader["id_osoby"]),
							Osoba = new Osoba
							{
								Id = Convert.ToInt32(reader["id_osoby"]),
								Imie = reader["imie"].ToString() ?? string.Empty,
								Nazwisko = reader["nazwisko"].ToString() ?? string.Empty,
								Telefony = new List<Telefon>()
							},
							IdAdresu = Convert.ToInt32(reader["id_adresu"])
						};

						lookup.Add(idKlienta, currentClient);
					}

					if (reader.IsDBNull(reader.GetOrdinal("id_telefonu"))) continue;
					var phone = new Telefon
					{
						Id = Convert.ToInt32(reader["id_telefonu"]),
						Numer = reader["numer"].ToString() ?? string.Empty,
						Opis = reader["opis"] as string
					};

					currentClient.Osoba.Telefony.Add(phone);
				}
			}
		}

		foreach (var klient in lookup.Values)
			if (addressRepository.GetAdresById(klient.IdAdresu) is { } adres)
				klient.Adres = adres;
		return lookup.Values;
	}

	public void DeleteById(int id)
	{
		if (id == 0) return;
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      DELETE FROM apteka.klienci WHERE id_klienta = ?;
		                      DELETE FROM apteka.osoby o
		                      WHERE 
		                          NOT EXISTS (
		                              SELECT 1 FROM apteka.klienci k 
		                              WHERE k.id_osoby_osoby = o.id_osoby 
		                          )
		                          AND NOT EXISTS (
		                              SELECT 1 FROM uzytkownicy.uzytkownicy u
		                              WHERE u.id_osoby_osoby = o.id_osoby
		                          )
		                          AND NOT EXISTS (
		                              SELECT 1 FROM apteka.lekarze l
		                              WHERE l.id_osoby_osoby = o.id_osoby
		                          );
		                      """;
		command.Parameters.Add(new OdbcParameter("@IdKlienta", id));
		command.ExecuteNonQuery();
		phoneRepository.RemoveUnusedPhones();
		addressRepository.RemoveUnusedAddresses();
	}

	public void AddOrUpdate(Klient klient)
	{
		if (klient.Id == 0) Add(klient);
		else Update(klient);
	}

	private void Add(Klient klient)
	{
		var idAdresu = addressRepository.AddOrUpdate(klient.Adres);
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      WITH new_osoba as (
		                          INSERT INTO apteka.osoby
		                          VALUES (DEFAULT, ?, ?) 
		                          RETURNING id_osoby
		                      )
		                      INSERT INTO apteka.klienci (pesel, id_adresu_adresy, id_osoby_osoby) 
		                      SELECT ?, ?, new_osoba.id_osoby
		                      FROM new_osoba
		                      RETURNING id_osoby_osoby;
		                      """;
		var osoba = klient.Osoba;
		command.Parameters.Add(new OdbcParameter("@Imie", osoba.Imie));
		command.Parameters.Add(new OdbcParameter("@Nazwisko", osoba.Nazwisko));
		command.Parameters.Add(new OdbcParameter("@Pesel", klient.Pesel));
		command.Parameters.Add(new OdbcParameter("@idAdresu", idAdresu));
		var idOsoby = Convert.ToInt32(command.ExecuteScalar());
		phoneRepository.AddOrUpdate(klient.Osoba.Telefony, idOsoby);
	}

	private void Update(Klient klient)
	{
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      UPDATE apteka.osoby SET imie = ?, nazwisko = ? WHERE id_osoby = ?;
		                      UPDATE apteka.klienci SET pesel = ? WHERE id_klienta = ?;
		                      """;
		addressRepository.AddOrUpdate(klient.Adres);
		var osoba = klient.Osoba;
		command.Parameters.Add(new OdbcParameter("@Imie", osoba.Imie));
		command.Parameters.Add(new OdbcParameter("@Nazwisko", osoba.Nazwisko));
		command.Parameters.Add(new OdbcParameter("@IdOsoby", osoba.Id));
		command.Parameters.Add(new OdbcParameter("@Pesel", klient.Pesel));
		command.Parameters.Add(new OdbcParameter("@IdKlienta", klient.Id));
		command.ExecuteNonQuery();
		phoneRepository.AddOrUpdate(klient.Osoba.Telefony, klient.IdOsoby);
	}
}