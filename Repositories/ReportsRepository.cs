using System;
using System.Collections.Generic;
using System.Data.Odbc;
using Apteka.Models;

namespace Apteka.Repositories;

public class ReportsRepository(DatabaseService dbService)
{
	public IEnumerable<Sprzedarz> GetSalesReport(DateTimeOffset startDate, DateTimeOffset endDate)
	{
		var result = new List<Sprzedarz>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT s.id_sprzedazy, s.data_sprzedazy, s.kwota 
		                      FROM apteka.sprzedaze s
		                      WHERE s.data_sprzedazy BETWEEN ? AND ?
		                      ORDER BY s.data_sprzedazy DESC
		                      """;
		command.Parameters.Add(new OdbcParameter("@StartDate", startDate.DateTime));
		command.Parameters.Add(new OdbcParameter("@EndDate", endDate.DateTime));
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
			{
				var kwotaBrutto = reader.IsDBNull(2) ? 0 : Convert.ToDecimal(reader["kwota"]);
				result.Add(new Sprzedarz
				{
					Id = Convert.ToInt32(reader["id_sprzedazy"]),
					Data = Convert.ToDateTime(reader["data_sprzedazy"]),
					KwotaBrutto = kwotaBrutto
				});
			}
		}

		return result;
	}

	public IEnumerable<StanMagazynu> GetDrugStock(DateTimeOffset startDate, DateTimeOffset endDate)
	{
		var result = new List<StanMagazynu>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT pelna_nazwa, ilosc_dostepna, najblizsza_data_waznosci 
		                      FROM magazyn.v_stan_magazynu_lekow
		                      ORDER BY pelna_nazwa
		                      """;
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
				result.Add(new StanMagazynu
				{
					PelnaNazwa = reader["pelna_nazwa"].ToString() ?? string.Empty,
					DostepnaIlosc = Convert.ToInt32(reader["ilosc_dostepna"]),
					DataWaznosci = reader.IsDBNull(2) ? null : Convert.ToDateTime(reader["najblizsza_data_waznosci"]),
					Typ = Typ.Lek
				});
		}

		command.CommandText = """
		                      SELECT nazwa_surowca, ilosc_dostepna, najblizsza_data_waznosci 
		                      FROM magazyn.v_stan_magazynu_surowcow
		                      ORDER BY nazwa_surowca
		                      """;
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
				result.Add(new StanMagazynu
				{
					PelnaNazwa = reader["nazwa_surowca"].ToString() ?? string.Empty,
					DostepnaIlosc = Convert.ToInt32(reader["ilosc_dostepna"]),
					DataWaznosci = reader.IsDBNull(2) ? null : Convert.ToDateTime(reader["najblizsza_data_waznosci"]),
					Typ = Typ.Surowiec
				});
		}

		return result;
	}

	public IEnumerable<StanMagazynu> GetStockAlerts(int minimumQuantity, DateTimeOffset expiryLimit)
	{
		var result = new List<StanMagazynu>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT pelna_nazwa, ilosc_dostepna, najblizsza_data_waznosci
		                      FROM magazyn.v_stan_magazynu_lekow
		                      WHERE ilosc_dostepna <= ? OR najblizsza_data_waznosci <= ?
		                      ORDER BY ilosc_dostepna, najblizsza_data_waznosci
		                      """;
		command.Parameters.Add(new OdbcParameter("@MinimumQuantity", minimumQuantity));
		command.Parameters.Add(new OdbcParameter("@ExpiryLimit", expiryLimit.DateTime));
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
				result.Add(new StanMagazynu
				{
					PelnaNazwa = reader["pelna_nazwa"].ToString() ?? string.Empty,
					DostepnaIlosc = Convert.ToInt32(reader["ilosc_dostepna"]),
					DataWaznosci = reader.IsDBNull(2) ? null : Convert.ToDateTime(reader["najblizsza_data_waznosci"]),
					Typ = Typ.Lek
				});
		}

		command.CommandText = """
		                      SELECT nazwa_surowca, ilosc_dostepna, najblizsza_data_waznosci
		                      FROM magazyn.v_stan_magazynu_surowcow
		                      WHERE ilosc_dostepna <= ? OR najblizsza_data_waznosci <= ?
		                      ORDER BY ilosc_dostepna, najblizsza_data_waznosci
		                      """;
		command.Parameters.Clear();
		command.Parameters.Add(new OdbcParameter("@MinimumQuantity", minimumQuantity));
		command.Parameters.Add(new OdbcParameter("@ExpiryLimit", expiryLimit.DateTime));
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
				result.Add(new StanMagazynu
				{
					PelnaNazwa = reader["nazwa_surowca"].ToString() ?? string.Empty,
					DostepnaIlosc = Convert.ToInt32(reader["ilosc_dostepna"]),
					DataWaznosci = reader.IsDBNull(2) ? null : Convert.ToDateTime(reader["najblizsza_data_waznosci"]),
					Typ = Typ.Surowiec
				});
		}

		return result;
	}

	public IEnumerable<AuditLogEntry> GetAuditLog(DateTimeOffset startDate, DateTimeOffset endDate)
	{
		var result = new List<AuditLogEntry>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT l.id_operacji, l.data_operacji, l.typ_operacji, l.encja,
		                             l.klucz_rekordu, l.opis,
		                             u.login, o.imie, o.nazwisko
		                      FROM uzytkownicy.log_operacji l
		                      LEFT JOIN uzytkownicy.uzytkownicy u ON u.id_uzytkownika = l.id_uzytkownika_uzytkownicy
		                      LEFT JOIN apteka.osoby o ON o.id_osoby = u.id_osoby_osoby
		                      WHERE l.data_operacji BETWEEN ? AND ?
		                      ORDER BY l.data_operacji DESC, l.id_operacji DESC
		                      LIMIT 1000;
		                      """;
		command.Parameters.Add(new OdbcParameter("@StartDate", startDate.DateTime));
		command.Parameters.Add(new OdbcParameter("@EndDate", endDate.DateTime));

		using var reader = command.ExecuteReader();
		while (reader.Read())
		{
			var imie = reader["imie"].ToString();
			var nazwisko = reader["nazwisko"].ToString();
			var fullName = string.IsNullOrWhiteSpace(imie) && string.IsNullOrWhiteSpace(nazwisko)
				? null
				: $"{imie} {nazwisko}".Trim();

			result.Add(new AuditLogEntry
			{
				Id = Convert.ToInt32(reader["id_operacji"]),
				DataOperacji = Convert.ToDateTime(reader["data_operacji"]),
				TypOperacji = reader["typ_operacji"].ToString() ?? string.Empty,
				Encja = reader["encja"].ToString() ?? string.Empty,
				KluczRekordu = reader["klucz_rekordu"] == DBNull.Value ? null : reader["klucz_rekordu"].ToString(),
				Opis = reader["opis"] == DBNull.Value ? null : reader["opis"].ToString(),
				Login = reader["login"] == DBNull.Value ? null : reader["login"].ToString(),
				Uzytkownik = fullName
			});
		}

		return result;
	}
}
