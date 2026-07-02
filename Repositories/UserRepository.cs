using System;
using System.Collections.Generic;
using System.Data.Odbc;
using Apteka.Models;
using Apteka.Services;

namespace Apteka.Repositories;

public class UserRepository(DatabaseService dbService)
{
	public IEnumerable<Uzytkownik> GetAll()
	{
		var users = new List<Uzytkownik>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT u.id_uzytkownika, u.login, u.ostatnie_logowanie, u.aktywny,
		                             r.nazwa_roli,
		                             o.id_osoby, o.imie, o.nazwisko
		                      FROM uzytkownicy.uzytkownicy u
		                      JOIN uzytkownicy.role r ON r.id_roli = u.id_roli_role
		                      JOIN apteka.osoby o ON o.id_osoby = u.id_osoby_osoby
		                      ORDER BY u.aktywny DESC, o.nazwisko, o.imie, u.login;
		                      """;

		using var reader = command.ExecuteReader();
		while (reader.Read())
			users.Add(new Uzytkownik
			{
				Id = Convert.ToInt32(reader["id_uzytkownika"]),
				Login = reader["login"].ToString() ?? string.Empty,
				Rola = reader["nazwa_roli"].ToString() ?? string.Empty,
				Aktywny = OdbcValue.ToBoolean(reader["aktywny"]),
				OstatnieLogowanie = reader["ostatnie_logowanie"] == DBNull.Value
					? null
					: Convert.ToDateTime(reader["ostatnie_logowanie"]),
				IdOsoby = Convert.ToInt32(reader["id_osoby"]),
				Osoba = new Osoba
				{
					Id = Convert.ToInt32(reader["id_osoby"]),
					Imie = reader["imie"].ToString() ?? string.Empty,
					Nazwisko = reader["nazwisko"].ToString() ?? string.Empty
				}
			});

		return users;
	}

	public IEnumerable<string> GetRoles()
	{
		var roles = new List<string>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "SELECT nazwa_roli FROM uzytkownicy.role ORDER BY nazwa_roli;";

		using var reader = command.ExecuteReader();
		while (reader.Read())
			roles.Add(reader["nazwa_roli"].ToString() ?? string.Empty);

		return roles;
	}

	public int AddOrUpdate(Uzytkownik user, string? newPassword)
	{
		ValidateUser(user, newPassword);

		using var connection = dbService.CreateConnection();
		using var transaction = connection.BeginTransaction();
		try
		{
			using var command = connection.CreateCommand();
			command.Transaction = transaction;

			var roleId = GetRoleId(command, user.Rola);
			if (user.Id == 0)
			{
				command.CommandText = """
				                      INSERT INTO apteka.osoby (imie, nazwisko)
				                      VALUES (?, ?)
				                      RETURNING id_osoby;
				                      """;
				command.Parameters.Add(new OdbcParameter("@Imie", user.Osoba.Imie.Trim()));
				command.Parameters.Add(new OdbcParameter("@Nazwisko", user.Osoba.Nazwisko.Trim()));
				user.IdOsoby = Convert.ToInt32(command.ExecuteScalar());
				command.Parameters.Clear();

				command.CommandText = """
				                      INSERT INTO uzytkownicy.uzytkownicy
				                          (login, haslo_hash, id_roli_role, id_osoby_osoby, aktywny)
				                      VALUES (?, ?, ?, ?, ?)
				                      RETURNING id_uzytkownika;
				                      """;
				command.Parameters.Add(new OdbcParameter("@Login", user.Login.Trim()));
				command.Parameters.Add(new OdbcParameter("@Password", PasswordHasher.Hash(newPassword!)));
				command.Parameters.Add(new OdbcParameter("@IdRoli", roleId));
				command.Parameters.Add(new OdbcParameter("@IdOsoby", user.IdOsoby));
				command.Parameters.Add(new OdbcParameter("@Aktywny", user.Aktywny));
				user.Id = Convert.ToInt32(command.ExecuteScalar());
			}
			else
			{
				command.CommandText = "UPDATE apteka.osoby SET imie = ?, nazwisko = ? WHERE id_osoby = ?;";
				command.Parameters.Add(new OdbcParameter("@Imie", user.Osoba.Imie.Trim()));
				command.Parameters.Add(new OdbcParameter("@Nazwisko", user.Osoba.Nazwisko.Trim()));
				command.Parameters.Add(new OdbcParameter("@IdOsoby", user.IdOsoby));
				command.ExecuteNonQuery();
				command.Parameters.Clear();

				if (string.IsNullOrWhiteSpace(newPassword))
				{
					command.CommandText = """
					                      UPDATE uzytkownicy.uzytkownicy
					                      SET login = ?, id_roli_role = ?, aktywny = ?
					                      WHERE id_uzytkownika = ?;
					                      """;
					command.Parameters.Add(new OdbcParameter("@Login", user.Login.Trim()));
					command.Parameters.Add(new OdbcParameter("@IdRoli", roleId));
					command.Parameters.Add(new OdbcParameter("@Aktywny", user.Aktywny));
					command.Parameters.Add(new OdbcParameter("@IdUzytkownika", user.Id));
				}
				else
				{
					command.CommandText = """
					                      UPDATE uzytkownicy.uzytkownicy
					                      SET login = ?, haslo_hash = ?, id_roli_role = ?, aktywny = ?
					                      WHERE id_uzytkownika = ?;
					                      """;
					command.Parameters.Add(new OdbcParameter("@Login", user.Login.Trim()));
					command.Parameters.Add(new OdbcParameter("@Password", PasswordHasher.Hash(newPassword)));
					command.Parameters.Add(new OdbcParameter("@IdRoli", roleId));
					command.Parameters.Add(new OdbcParameter("@Aktywny", user.Aktywny));
					command.Parameters.Add(new OdbcParameter("@IdUzytkownika", user.Id));
				}

				command.ExecuteNonQuery();
			}

			transaction.Commit();
			return user.Id;
		}
		catch
		{
			transaction.Rollback();
			throw;
		}
	}

	private static int GetRoleId(System.Data.IDbCommand command, string role)
	{
		command.Parameters.Clear();
		command.CommandText = "SELECT id_roli FROM uzytkownicy.role WHERE nazwa_roli = ?;";
		command.Parameters.Add(new OdbcParameter("@Rola", role));
		var result = command.ExecuteScalar();
		command.Parameters.Clear();
		if (result is null || result == DBNull.Value)
			throw new InvalidOperationException("Wybrana rola nie istnieje.");

		return Convert.ToInt32(result);
	}

	private static void ValidateUser(Uzytkownik user, string? newPassword)
	{
		if (string.IsNullOrWhiteSpace(user.Login))
			throw new InvalidOperationException("Login jest wymagany.");
		if (string.IsNullOrWhiteSpace(user.Rola))
			throw new InvalidOperationException("Rola jest wymagana.");
		if (string.IsNullOrWhiteSpace(user.Osoba.Imie) || string.IsNullOrWhiteSpace(user.Osoba.Nazwisko))
			throw new InvalidOperationException("Imię i nazwisko pracownika są wymagane.");
		if (user.Id == 0 && string.IsNullOrWhiteSpace(newPassword))
			throw new InvalidOperationException("Nowy użytkownik musi mieć hasło.");
		if (!string.IsNullOrWhiteSpace(newPassword) && newPassword.Length < 8)
			throw new InvalidOperationException("Hasło musi mieć co najmniej 8 znaków.");
	}
}
