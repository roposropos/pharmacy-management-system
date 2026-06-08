using System;
using System.Security.Cryptography;
using System.Text;
using Apteka.Models;

namespace Apteka.Repositories;

public class LoginRepository(DatabaseService dbService)
{
	public Uzytkownik? ValidateUser(string username, string password)
	{
		using var db = dbService.CreateConnection();
		using var dbCommand = db.CreateCommand();
		dbCommand.CommandText = """
		                        SELECT
		                            u.id_uzytkownika, u.login, o.id_osoby, o.imie, o.nazwisko, r.nazwa_roli
		                        FROM uzytkownicy.uzytkownicy u
		                                 JOIN apteka.osoby o ON u.id_osoby_osoby = o.id_osoby
		                                 LEFT JOIN uzytkownicy.role r on u.id_roli_role = r.id_roli
		                        WHERE u.login = ? AND u.haslo_hash = ?;
		                        """;
		var login = dbCommand.CreateParameter();
		login.ParameterName = "@Username";
		login.Value = username;
		dbCommand.Parameters.Add(login);

		var passwordHash = HashPassword(password);

		var pass = dbCommand.CreateParameter();
		pass.ParameterName = "@Password";
		pass.Value = passwordHash;
		dbCommand.Parameters.Add(pass);


		using var reader = dbCommand.ExecuteReader();
		Uzytkownik? user = null;
		if (reader.Read())
			user = new Uzytkownik
			{
				Id = Convert.ToInt32(reader["id_uzytkownika"]),
				Login = reader["login"].ToString() ?? string.Empty,
				Rola = reader["nazwa_roli"].ToString() ?? string.Empty,
				IdOsoby = Convert.ToInt32(reader["id_osoby"]),
				Osoba = new Osoba
				{
					Id = Convert.ToInt32(reader["id_osoby"]),
					Imie = reader["imie"].ToString() ?? string.Empty,
					Nazwisko = reader["nazwisko"].ToString() ?? string.Empty
				}
			};
		reader.Close();
		if (user == null) return null;

		using var dbLoginTimestamp = db.CreateCommand();
		dbLoginTimestamp.CommandText =
			"UPDATE uzytkownicy.uzytkownicy SET ostatnie_logowanie = CURRENT_DATE WHERE id_uzytkownika = ?";
		var id = dbLoginTimestamp.CreateParameter();
		id.ParameterName = "@Id";
		id.Value = user.Id;
		dbLoginTimestamp.Parameters.Add(id);
		dbLoginTimestamp.ExecuteNonQuery();

		dbService.SetCurrentUser(user.Id);
		return user;
	}

	private static string HashPassword(string password)
	{
		var passwordBytes = Encoding.UTF8.GetBytes(password);
		var hashed = SHA256.HashData(passwordBytes);
		return Convert.ToBase64String(hashed);
	}
}