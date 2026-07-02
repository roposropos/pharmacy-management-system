using System;
using System.Collections.Generic;
using System.Data.Odbc;
using Apteka.Models;

namespace Apteka.Repositories;

public class PhoneRepository(DatabaseService dbService)
{
	public void DeletePhone(int idOsoby)
	{
		foreach (var id in FindIds(idOsoby)) DeletePhone(idOsoby, id);
	}

	public void DeletePhone(int idOsoby, int idTelefonu)
	{
		using var connection = dbService.CreateConnection();
		using var transaction = connection.BeginTransaction();
		try
		{
			using var command = connection.CreateCommand();
			command.CommandText = """
			                      DELETE FROM apteka.pozycja_numeru
			                      WHERE id_osoby_osoby = ? AND id_telefonu_numery_telefonu = ?;
			                      """;
			command.Parameters.Add(new OdbcParameter("@IdOsoby", idOsoby));
			command.Parameters.Add(new OdbcParameter("@IdTelefonu", idTelefonu));
			command.ExecuteNonQuery();
			RemoveUnusedPhones();
			transaction.Commit();
		}
		catch
		{
			transaction.Rollback();
			throw;
		}
	}

	public void RemoveUnusedPhones()
	{
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      DELETE FROM apteka.numery_telefonu
		                      WHERE id_telefonu NOT IN (SELECT id_telefonu_numery_telefonu FROM apteka.pozycja_numeru);
		                      """;
		command.ExecuteNonQuery();
	}

	public void AddOrUpdate(IEnumerable<Telefon> telefony, int clientId)
	{
		foreach (var telefon in telefony) AddOrUpdate(telefon, clientId);
	}

	public void AddOrUpdate(Telefon telefon, int clientId)
	{
		if (telefon.Id == 0)
		{
			var id = AddNumber(telefon);
			AddRecord(clientId, id);
			return;
		}

		Update(telefon);

		if (CheckIfExists(clientId, telefon.Id)) return;
		AddRecord(clientId, telefon.Id);
	}

	private void Update(Telefon telefon)
	{
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "UPDATE apteka.numery_telefonu SET numer = ?, opis = ? WHERE id_telefonu = ?;";
		command.Parameters.Add(new OdbcParameter("@Numer", telefon.Numer));
		command.Parameters.Add(new OdbcParameter("@opis", telefon.Opis));
		command.Parameters.Add(new OdbcParameter("@IdTelefonu", telefon.Id));
		command.ExecuteNonQuery();
	}

	private List<int> FindIds(int idOsoby)
	{
		var ids = new List<int>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "SELECT id_telefonu_numery_telefonu FROM apteka.pozycja_numeru WHERE id_osoby_osoby = ?;";
		command.Parameters.Add(new OdbcParameter("@IdOsoby", idOsoby));
		using var reader = command.ExecuteReader();
		while (reader.Read()) ids.Add(reader.GetInt32(0));
		return ids;
	}

	private bool CheckIfExists(int idOsoby, int idTelefonu)
	{
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText =
			"SELECT 1 FROM apteka.pozycja_numeru WHERE id_osoby_osoby = ? AND id_telefonu_numery_telefonu = ?;";
		command.Parameters.Add(new OdbcParameter("@IdOsoby", idOsoby));
		command.Parameters.Add(new OdbcParameter("@IdTelefonu", idTelefonu));
		return command.ExecuteScalar() != null;
	}

	private void AddRecord(int idOsoby, int idTelefonu)
	{
		if (idTelefonu == 0) return;
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText =
			"INSERT INTO apteka.pozycja_numeru (id_osoby_osoby, id_telefonu_numery_telefonu) VALUES (?, ?);";
		command.Parameters.Add(new OdbcParameter("@IdOsoby", idOsoby));
		command.Parameters.Add(new OdbcParameter("@IdTelefonu", idTelefonu));
		command.ExecuteNonQuery();
	}

	private int AddNumber(Telefon telefon)
	{
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "INSERT INTO apteka.numery_telefonu (numer, opis) VALUES (?, ?);";
		command.Parameters.Add(new OdbcParameter("@Numer", telefon.Numer));
		command.Parameters.Add(new OdbcParameter("@Opis", telefon.Opis));
		command.ExecuteNonQuery();
		command.CommandText = "SELECT lastval()";
		return Convert.ToInt32(command.ExecuteScalar());
	}
}