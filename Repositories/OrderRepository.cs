using System;
using System.Collections.Generic;
using System.Data.Odbc;
using Apteka.Models;

namespace Apteka.Repositories;

public class OrderRepository(DatabaseService dbService, AddressRepository addressRepository)
{
	public IEnumerable<Zamowienie> GetAll()
	{
		var orders = new List<Zamowienie>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT z.id_zamowienia, z.data_utworzenia, z.status,  z.typ,
		                             d.id_dostawcy, d.nazwa, d."NIP", d.id_adresu_adresy
		                      FROM magazyn.zamowienia z
		                      JOIN magazyn.dostawcy d on z.id_dostawcy_dostawcy = d.id_dostawcy
		                      ORDER BY data_utworzenia DESC;
		                      """;

		using var reader = command.ExecuteReader();
		while (reader.Read())
			orders.Add(new Zamowienie
			{
				Id = Convert.ToInt32(reader["id_zamowienia"]),
				DataUtworzenia = Convert.ToDateTime(reader["data_utworzenia"]),
				Status = reader["status"].ToString() ?? string.Empty,
				Typ = reader["typ"].ToString() ?? string.Empty,
				IdDostawcy = Convert.ToInt32(reader["id_dostawcy"]),
				Dostawca = new Dostawca
				{
					Id = Convert.ToInt32(reader["id_dostawcy"]),
					Nazwa = reader["nazwa"].ToString() ?? string.Empty,
					NIP = reader["NIP"].ToString() ?? string.Empty,
					IdAdresu = Convert.ToInt32(reader["id_adresu_adresy"])
				}
			});

		foreach (var order in orders)
		{
			var adres = addressRepository.GetAdresById(order.IdDostawcy);
			order.Dostawca!.Adres = adres;
		}

		return orders;
	}

	public void Add(Zamowienie zamowienie)
	{
		using var connection = dbService.CreateConnection();
		using var transaction = connection.BeginTransaction();
		try
		{
			using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = """
			                      INSERT INTO magazyn.zamowienia (data_utworzenia, status, typ, id_dostawcy_dostawcy) 
			                      VALUES (?, ?, ?, ?) 
			                      RETURNING id_zamowienia;
			                      """;
			command.Parameters.Add(new OdbcParameter("@Data", zamowienie.DataUtworzenia));
			command.Parameters.Add(new OdbcParameter("@Status", zamowienie.Status));
			command.Parameters.Add(new OdbcParameter("@Typ", zamowienie.Typ));
			command.Parameters.Add(new OdbcParameter("@IdDostawcy", zamowienie.IdDostawcy));
			zamowienie.Id = Convert.ToInt32(command.ExecuteScalar());

			transaction.Commit();
		}
		catch
		{
			transaction.Rollback();
			throw;
		}
	}
}