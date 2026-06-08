using System;
using System.Collections.Generic;
using System.Data.Odbc;
using Apteka.Models;

namespace Apteka.Repositories;

public class DeliveryRepository(DatabaseService dbService, AddressRepository addressRepository)
{
	public IEnumerable<Dostawa> GetAll()
	{
		var deliveries = new List<Dostawa>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """"
		                      		SELECT d.id_dostawy, d.data_dostawy, d.id_dostawcy_dostawcy,
		                      		       dos.nazwa, dos."NIP", dos.id_adresu_adresy
		                      		FROM magazyn.dostawy d
		                      		JOIN magazyn.dostawcy dos ON dos.id_dostawcy = d.id_dostawcy_dostawcy
		                      		ORDER BY data_dostawy DESC
		                      """";

		using var reader = command.ExecuteReader();
		while (reader.Read())
			deliveries.Add(new Dostawa
			{
				Id = Convert.ToInt32(reader["id_dostawy"]),
				DataDostawy = Convert.ToDateTime(reader["data_dostawy"]),
				IdDostawcy = Convert.ToInt32(reader["id_dostawcy_dostawcy"]),
				Dostawca = new Dostawca
				{
					Id = Convert.ToInt32(reader["id_dostawcy_dostawcy"]),
					Nazwa = reader["nazwa"].ToString() ?? string.Empty,
					NIP = reader["NIP"].ToString() ?? string.Empty,
					IdAdresu = Convert.ToInt32(reader["id_adresu_adresy"])
				}
			});

		foreach (var dostawa in deliveries)
		{
			var adres = addressRepository.GetAdresById(dostawa.Dostawca!.IdAdresu);
			dostawa.Dostawca.Adres = adres;
		}

		return deliveries;
	}

	public void Add(Dostawa dostawa)
	{
		using var connection = dbService.CreateConnection();
		using var transaction = connection.BeginTransaction();
		try
		{
			using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = """
			                      INSERT INTO magazyn.dostawy (data_dostawy, id_dostawcy_dostawcy) 
			                      VALUES (?, ?) 
			                      RETURNING id_dostawy
			                      """;
			command.Parameters.Add(new OdbcParameter("@Data", dostawa.DataDostawy));
			command.Parameters.Add(new OdbcParameter("@IdDostawcy", dostawa.IdDostawcy));

			dostawa.Id = Convert.ToInt32(command.ExecuteScalar());
			transaction.Commit();
		}
		catch
		{
			transaction.Rollback();
			throw;
		}
	}
}