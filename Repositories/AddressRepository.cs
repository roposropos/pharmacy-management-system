using System;
using System.Collections.Generic;
using System.Data.Odbc;
using Apteka.Models;

namespace Apteka.Repositories;

public class AddressRepository(DatabaseService dbService)
{
	public Adres? GetAdresById(int id)
	{
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "SELECT * FROM apteka.adresy a WHERE a.id_adresu = ?";
		var idAdresu = command.CreateParameter();
		idAdresu.ParameterName = "@IdAdresu";
		idAdresu.Value = id;
		command.Parameters.Add(idAdresu);
		using var reader = command.ExecuteReader();
		if (reader.Read())
		{
			var adres = new Adres
			{
				Id = id,
				Ulica = reader.IsDBNull(1) ? null : reader.GetString(1),
				NumerDomu = reader.GetString(2),
				NumerLokalu = reader.IsDBNull(3) ? null : reader.GetString(3),
				KodPocztowy = reader.GetString(4),
				Miejscowosc = reader.GetString(5),
				Kraj = reader.GetString(6)
			};
			return adres;
		}

		return null;
	}

	public IEnumerable<Adres> GetAll()
	{
		var addresses = new List<Adres>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "SELECT * FROM apteka.adresy a WHERE a.id_adresu = ?";
		using var reader = command.ExecuteReader();
		while (reader.Read())
		{
			var adres = new Adres
			{
				Id = reader.GetInt32(0),
				Ulica = reader.IsDBNull(1) ? null : reader.GetString(1),
				NumerDomu = reader.GetString(2),
				NumerLokalu = reader.IsDBNull(3) ? null : reader.GetString(3),
				KodPocztowy = reader.GetString(4),
				Miejscowosc = reader.GetString(5),
				Kraj = reader.GetString(6)
			};
			addresses.Add(adres);
		}

		return addresses;
	}

	public void RemoveUnusedAddresses()
	{
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      DELETE FROM apteka.adresy a
		                      WHERE 
		                          NOT EXISTS (
		                              SELECT 1 FROM apteka.producenci p 
		                              WHERE p.id_adresu_adresy = a.id_adresu
		                          )
		                          AND NOT EXISTS (
		                              SELECT 1 FROM apteka.klienci k 
		                              WHERE k.id_adresu_adresy = a.id_adresu
		                          )
		                          AND NOT EXISTS (
		                              SELECT 1 FROM magazyn.dostawcy d
		                              WHERE d.id_adresu_adresy = a.id_adresu
		                          );
		                      """;
		command.ExecuteNonQuery();
	}

	public int AddOrUpdate(Adres adres)
	{
		return adres.Id == 0 ? Add(adres) : Update(adres);
	}

	private int Update(Adres adres)
	{
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText =
			"UPDATE apteka.adresy SET ulica = ?, nr_domu = ?, nr_lokalu = ?, kod_pocztowy = ?, miasto = ?,kraj = ? WHERE id_adresu = ?;";
		command.Parameters.Add(new OdbcParameter("@Ulica", adres.Ulica));
		command.Parameters.Add(new OdbcParameter("@NrDomu", adres.NumerDomu));
		command.Parameters.Add(new OdbcParameter("@NrLokalu", adres.NumerLokalu));
		command.Parameters.Add(new OdbcParameter("@KodPocztowy", adres.KodPocztowy));
		command.Parameters.Add(new OdbcParameter("@Miejscowosc", adres.Miejscowosc));
		command.Parameters.Add(new OdbcParameter("@Kraj", adres.Kraj));
		command.Parameters.Add(new OdbcParameter("@IdAdresu", adres.Id));
		command.ExecuteNonQuery();
		return adres.Id;
	}

	private int Add(Adres adres)
	{
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      	INSERT INTO apteka.adresy 
		                      	(ulica, nr_domu, nr_lokalu, kod_pocztowy, miasto, kraj) 
		                      	VALUES (?, ?, ?, ?, ?, ?);
		                      """;
		command.Parameters.Add(new OdbcParameter("@Ulica", adres.Ulica));
		command.Parameters.Add(new OdbcParameter("@NrDomu", adres.NumerDomu));
		command.Parameters.Add(new OdbcParameter("@NrLokalu", adres.NumerLokalu));
		command.Parameters.Add(new OdbcParameter("@KodPocztowy", adres.KodPocztowy));
		command.Parameters.Add(new OdbcParameter("@Miejscowosc", adres.Miejscowosc));
		command.Parameters.Add(new OdbcParameter("@Kraj", adres.Kraj));
		command.ExecuteNonQuery();
		command.CommandText = "SELECT lastval()";
		return Convert.ToInt32(command.ExecuteScalar());
	}
}