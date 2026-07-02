using System;
using System.Collections.Generic;
using System.Data.Odbc;
using Apteka.Models;

namespace Apteka.Repositories;

public class InventoryRepository(DatabaseService dbService)
{
	public IEnumerable<StanPartiiLeku> GetDrugBatches()
	{
		var batches = new List<StanPartiiLeku>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT pl.id_partii, pl.numer_partii, pl.data_waznosci,
		                             pl.ilosc_dostepna, pl.ilosc_zarezerwowana,
		                             w.id_wariantu, w.kod_ean, w.postac, w.dawkowanie, w.ilosc AS ilosc_w_opakowaniu,
		                             l.id_leku, l.nazwa, l.substancja_czynna,
		                             p.id_producenta, p.nazwa AS nazwa_producenta
		                      FROM magazyn.partie_lekow pl
		                      JOIN apteka.warianty_lekow w ON w.id_wariantu = pl.id_wariantu_warianty_lekow
		                      JOIN apteka.leki l ON l.id_leku = w.id_leku_leki
		                      JOIN apteka.producenci p ON p.id_producenta = l.id_producenta_producenci
		                      ORDER BY l.nazwa, w.dawkowanie, pl.data_waznosci, pl.numer_partii;
		                      """;

		using var reader = command.ExecuteReader();
		while (reader.Read())
			batches.Add(new StanPartiiLeku
			{
				IdPartii = Convert.ToInt32(reader["id_partii"]),
				IdWariantu = Convert.ToInt32(reader["id_wariantu"]),
				IdLeku = Convert.ToInt32(reader["id_leku"]),
				IdProducenta = Convert.ToInt32(reader["id_producenta"]),
				NazwaLeku = reader["nazwa"].ToString() ?? string.Empty,
				NazwaProducenta = reader["nazwa_producenta"].ToString() ?? string.Empty,
				SubstancjaCzynna = reader["substancja_czynna"].ToString() ?? string.Empty,
				KodEan = Convert.ToInt64(reader["kod_ean"]),
				Postac = (PostacLeku)Convert.ToInt16(reader["postac"]),
				Dawka = reader["dawkowanie"].ToString() ?? string.Empty,
				IloscWOpakowaniu = Convert.ToInt32(reader["ilosc_w_opakowaniu"]),
				NumerPartii = reader["numer_partii"].ToString() ?? string.Empty,
				DataWaznosci = Convert.ToDateTime(reader["data_waznosci"]),
				IloscDostepna = Convert.ToInt32(reader["ilosc_dostepna"]),
				IloscZarezerwowana = Convert.ToInt32(reader["ilosc_zarezerwowana"])
			});

		return batches;
	}

	public int AdjustBatchQuantity(int batchId, int delta, string reason)
	{
		if (batchId <= 0) throw new InvalidOperationException("Wybierz partię do korekty.");
		if (delta == 0) throw new InvalidOperationException("Korekta musi zmieniać ilość.");

		using var connection = dbService.CreateConnection();
		using var transaction = connection.BeginTransaction();
		try
		{
			using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = """
			                      UPDATE magazyn.partie_lekow
			                      SET ilosc_dostepna = ilosc_dostepna + ?
			                      WHERE id_partii = ?
			                        AND ilosc_dostepna + ? >= ilosc_zarezerwowana
			                      RETURNING ilosc_dostepna;
			                      """;
			command.Parameters.Add(new OdbcParameter("@Delta", delta));
			command.Parameters.Add(new OdbcParameter("@IdPartii", batchId));
			command.Parameters.Add(new OdbcParameter("@DeltaCheck", delta));
			var newValue = command.ExecuteScalar();
			if (newValue is null || newValue == DBNull.Value)
				throw new InvalidOperationException("Nie można wykonać korekty. Stan po korekcie nie może spaść poniżej rezerwacji.");

			command.Parameters.Clear();
			command.CommandText = """
			                      INSERT INTO uzytkownicy.log_operacji
			                          (typ_operacji, encja, klucz_rekordu, opis, id_uzytkownika_uzytkownicy)
			                      VALUES ('KOREKTA', 'magazyn.partie_lekow', ?, ?,
			                              NULLIF(current_setting('app.user_id', true), '')::integer);
			                      """;
			command.Parameters.Add(new OdbcParameter("@IdPartii", batchId.ToString()));
			command.Parameters.Add(new OdbcParameter("@Opis",
				$"Korekta stanu partii o {delta}. Powód: {(string.IsNullOrWhiteSpace(reason) ? "brak opisu" : reason.Trim())}"));
			command.ExecuteNonQuery();

			transaction.Commit();
			return Convert.ToInt32(newValue);
		}
		catch
		{
			transaction.Rollback();
			throw;
		}
	}
}
