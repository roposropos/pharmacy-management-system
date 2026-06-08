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
		try
		{
			using var reader = command.ExecuteReader();
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
		catch (Exception ex)
		{
			Console.WriteLine($"Error fetching sales report: {ex.Message}");
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
		                      WHERE najblizsza_data_waznosci BETWEEN ? AND ?
		                      ORDER BY pelna_nazwa
		                      """;
		command.Parameters.Add(new OdbcParameter("@StartDate", startDate.DateTime));
		command.Parameters.Add(new OdbcParameter("@EndDate", endDate.DateTime));
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
		                      WHERE najblizsza_data_waznosci BETWEEN ? AND ?
		                      ORDER BY nazwa_surowca
		                      """;
		command.Parameters.Add(new OdbcParameter("@StartDate", startDate.DateTime));
		command.Parameters.Add(new OdbcParameter("@EndDate", endDate.DateTime));
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
}