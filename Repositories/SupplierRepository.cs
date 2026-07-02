using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using Apteka.Models;

namespace Apteka.Repositories;

public class SupplierRepository(DatabaseService dbService)
{
	public IEnumerable<Dostawca> GetAll()
	{
		var suppliers = new List<Dostawca>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT d.id_dostawcy, d.nazwa, d."NIP", d.id_adresu_adresy,
		                             a.ulica, a.nr_domu, a.nr_lokalu, a.kod_pocztowy, a.miasto, a.kraj
		                      FROM magazyn.dostawcy d
		                      JOIN apteka.adresy a ON a.id_adresu = d.id_adresu_adresy
		                      ORDER BY d.nazwa;
		                      """;

		using var reader = command.ExecuteReader();
		while (reader.Read())
		{
			var idAdresu = Convert.ToInt32(reader["id_adresu_adresy"]);
			suppliers.Add(new Dostawca
			{
				Id = Convert.ToInt32(reader["id_dostawcy"]),
				Nazwa = reader["nazwa"].ToString() ?? string.Empty,
				NIP = reader["NIP"].ToString() ?? string.Empty,
				IdAdresu = idAdresu,
				Adres = new Adres
				{
					Id = idAdresu,
					Ulica = reader["ulica"] == DBNull.Value ? null : reader["ulica"].ToString(),
					NumerDomu = reader["nr_domu"].ToString() ?? string.Empty,
					NumerLokalu = reader["nr_lokalu"] == DBNull.Value ? null : reader["nr_lokalu"].ToString(),
					KodPocztowy = reader["kod_pocztowy"].ToString() ?? string.Empty,
					Miejscowosc = reader["miasto"].ToString() ?? string.Empty,
					Kraj = reader["kraj"].ToString() ?? string.Empty
				}
			});
		}

		return suppliers;
	}

	public int AddOrUpdate(Dostawca supplier)
	{
		ValidateSupplier(supplier);

		using var connection = dbService.CreateConnection();
		using var transaction = connection.BeginTransaction();
		try
		{
			using var command = connection.CreateCommand();
			command.Transaction = transaction;

			var addressId = AddOrUpdateAddress(command, supplier.Adres!);
			supplier.IdAdresu = addressId;

			if (supplier.Id == 0)
			{
				command.CommandText = """
				                      INSERT INTO magazyn.dostawcy (nazwa, "NIP", id_adresu_adresy)
				                      VALUES (?, ?, ?)
				                      RETURNING id_dostawcy;
				                      """;
				command.Parameters.Add(new OdbcParameter("@Nazwa", supplier.Nazwa.Trim()));
				command.Parameters.Add(new OdbcParameter("@NIP", supplier.NIP.Trim()));
				command.Parameters.Add(new OdbcParameter("@IdAdresu", addressId));
				supplier.Id = Convert.ToInt32(command.ExecuteScalar());
			}
			else
			{
				command.CommandText = """
				                      UPDATE magazyn.dostawcy
				                      SET nazwa = ?, "NIP" = ?, id_adresu_adresy = ?
				                      WHERE id_dostawcy = ?;
				                      """;
				command.Parameters.Add(new OdbcParameter("@Nazwa", supplier.Nazwa.Trim()));
				command.Parameters.Add(new OdbcParameter("@NIP", supplier.NIP.Trim()));
				command.Parameters.Add(new OdbcParameter("@IdAdresu", addressId));
				command.Parameters.Add(new OdbcParameter("@IdDostawcy", supplier.Id));
				command.ExecuteNonQuery();
			}

			transaction.Commit();
			return supplier.Id;
		}
		catch
		{
			transaction.Rollback();
			throw;
		}
	}

	public void Delete(int supplierId)
	{
		if (supplierId <= 0) return;

		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "DELETE FROM magazyn.dostawcy WHERE id_dostawcy = ?;";
		command.Parameters.Add(new OdbcParameter("@IdDostawcy", supplierId));
		command.ExecuteNonQuery();
	}

	private static int AddOrUpdateAddress(IDbCommand command, Adres address)
	{
		command.Parameters.Clear();
		if (address.Id == 0)
		{
			command.CommandText = """
			                      INSERT INTO apteka.adresy (ulica, nr_domu, nr_lokalu, kod_pocztowy, miasto, kraj)
			                      VALUES (?, ?, ?, ?, ?, ?)
			                      RETURNING id_adresu;
			                      """;
			AddAddressParameters(command, address);
			var id = Convert.ToInt32(command.ExecuteScalar());
			command.Parameters.Clear();
			return id;
		}

		command.CommandText = """
		                      UPDATE apteka.adresy
		                      SET ulica = ?, nr_domu = ?, nr_lokalu = ?, kod_pocztowy = ?, miasto = ?, kraj = ?
		                      WHERE id_adresu = ?;
		                      """;
		AddAddressParameters(command, address);
		command.Parameters.Add(new OdbcParameter("@IdAdresu", address.Id));
		command.ExecuteNonQuery();
		command.Parameters.Clear();
		return address.Id;
	}

	private static void AddAddressParameters(IDbCommand command, Adres address)
	{
		command.Parameters.Add(new OdbcParameter("@Ulica", string.IsNullOrWhiteSpace(address.Ulica) ? DBNull.Value : address.Ulica.Trim()));
		command.Parameters.Add(new OdbcParameter("@NrDomu", address.NumerDomu.Trim()));
		command.Parameters.Add(new OdbcParameter("@NrLokalu", string.IsNullOrWhiteSpace(address.NumerLokalu) ? DBNull.Value : address.NumerLokalu.Trim()));
		command.Parameters.Add(new OdbcParameter("@KodPocztowy", address.KodPocztowy.Trim()));
		command.Parameters.Add(new OdbcParameter("@Miejscowosc", address.Miejscowosc.Trim()));
		command.Parameters.Add(new OdbcParameter("@Kraj", address.Kraj.Trim()));
	}

	private static void ValidateSupplier(Dostawca supplier)
	{
		if (string.IsNullOrWhiteSpace(supplier.Nazwa))
			throw new InvalidOperationException("Nazwa dostawcy jest wymagana.");
		if (string.IsNullOrWhiteSpace(supplier.NIP))
			throw new InvalidOperationException("NIP dostawcy jest wymagany.");
		if (supplier.Adres is null)
			throw new InvalidOperationException("Adres dostawcy jest wymagany.");
		if (string.IsNullOrWhiteSpace(supplier.Adres.NumerDomu)
		    || string.IsNullOrWhiteSpace(supplier.Adres.KodPocztowy)
		    || string.IsNullOrWhiteSpace(supplier.Adres.Miejscowosc)
		    || string.IsNullOrWhiteSpace(supplier.Adres.Kraj))
			throw new InvalidOperationException("Adres dostawcy musi zawierać kraj, miasto, kod pocztowy i numer domu.");
	}
}
