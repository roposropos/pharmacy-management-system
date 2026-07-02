using System;
using Apteka.Models;
using Apteka.Services;

namespace Apteka.Repositories;

public class LoginRepository(DatabaseService dbService)
{
	public Uzytkownik? ValidateUser(string username, string password)
	{
		using var db = dbService.CreateConnection();
		using var dbCommand = db.CreateCommand();
		dbCommand.CommandText = """
		                        SELECT
		                            u.id_uzytkownika, u.login, u.haslo_hash, u.ostatnie_logowanie, u.aktywny,
		                            o.id_osoby, o.imie, o.nazwisko, r.nazwa_roli
		                        FROM uzytkownicy.uzytkownicy u
		                                 JOIN apteka.osoby o ON u.id_osoby_osoby = o.id_osoby
		                                 LEFT JOIN uzytkownicy.role r on u.id_roli_role = r.id_roli
		                        WHERE u.login = ? AND u.aktywny = true;
		                        """;
		var login = dbCommand.CreateParameter();
		login.ParameterName = "@Username";
		login.Value = username;
		dbCommand.Parameters.Add(login);

		using var reader = dbCommand.ExecuteReader();
		Uzytkownik? user = null;
		if (reader.Read() && PasswordHasher.Verify(password, reader["haslo_hash"].ToString() ?? string.Empty))
			user = new Uzytkownik
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
}
