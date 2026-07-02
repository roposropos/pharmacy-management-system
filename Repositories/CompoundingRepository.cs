using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using Apteka.Models;

namespace Apteka.Repositories;

public class CompoundingRepository(DatabaseService dbService)
{
	public IEnumerable<Surowiec> GetRawMaterials()
	{
		var result = new List<Surowiec>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT s.id_surowca, s.nazwa_surowca, s.typ, s.jednostka,
		                             COALESCE(SUM(
		                                 CASE
		                                     WHEN ps.data_waznosci >= CURRENT_DATE
		                                     THEN ps.ilosc_dostepna - ps.ilosc_zarezerwowana
		                                     ELSE 0
		                                 END
		                             ), 0) AS ilosc_dostepna,
		                             MIN(ps.data_waznosci) FILTER (
		                                 WHERE ps.data_waznosci >= CURRENT_DATE
		                                   AND (ps.ilosc_dostepna - ps.ilosc_zarezerwowana) > 0
		                             ) AS najblizsza_data_waznosci
		                      FROM magazyn.surowce s
		                      LEFT JOIN magazyn.partie_surowcow ps ON ps.id_surowca_surowce = s.id_surowca
		                      GROUP BY s.id_surowca, s.nazwa_surowca, s.typ, s.jednostka
		                      ORDER BY s.nazwa_surowca;
		                      """;

		using var reader = command.ExecuteReader();
		while (reader.Read())
			result.Add(new Surowiec
			{
				Id = Convert.ToInt32(reader["id_surowca"]),
				Nazwa = reader["nazwa_surowca"].ToString() ?? string.Empty,
				Typ = reader["typ"].ToString() ?? string.Empty,
				Jednostka = reader["jednostka"].ToString() ?? string.Empty,
				DostepnaIlosc = Convert.ToDecimal(reader["ilosc_dostepna"]),
				NajblizszaDataWaznosci = reader["najblizsza_data_waznosci"] == DBNull.Value
					? null
					: Convert.ToDateTime(reader["najblizsza_data_waznosci"])
			});

		return result;
	}

	public IEnumerable<PartiaSurowca> GetRawMaterialBatches()
	{
		var result = new List<PartiaSurowca>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT ps.id_partii_surowca, ps.numer_partii, ps.data_waznosci,
		                             ps.ilosc_dostepna, ps.ilosc_zarezerwowana,
		                             s.id_surowca, s.nazwa_surowca, s.jednostka
		                      FROM magazyn.partie_surowcow ps
		                      JOIN magazyn.surowce s ON s.id_surowca = ps.id_surowca_surowce
		                      ORDER BY s.nazwa_surowca, ps.data_waznosci, ps.numer_partii;
		                      """;

		using var reader = command.ExecuteReader();
		while (reader.Read())
			result.Add(new PartiaSurowca
			{
				Id = Convert.ToInt32(reader["id_partii_surowca"]),
				IdSurowca = Convert.ToInt32(reader["id_surowca"]),
				NazwaSurowca = reader["nazwa_surowca"].ToString() ?? string.Empty,
				Jednostka = reader["jednostka"].ToString() ?? string.Empty,
				NumerPartii = reader["numer_partii"].ToString() ?? string.Empty,
				DataWaznosci = Convert.ToDateTime(reader["data_waznosci"]),
				IloscDostepna = Convert.ToDecimal(reader["ilosc_dostepna"]),
				IloscZarezerwowana = Convert.ToDecimal(reader["ilosc_zarezerwowana"])
			});

		return result;
	}

	public int AddOrUpdateRawMaterial(Surowiec surowiec)
	{
		if (string.IsNullOrWhiteSpace(surowiec.Nazwa))
			throw new InvalidOperationException("Nazwa surowca jest wymagana.");
		if (string.IsNullOrWhiteSpace(surowiec.Typ))
			throw new InvalidOperationException("Typ surowca jest wymagany.");
		if (string.IsNullOrWhiteSpace(surowiec.Jednostka))
			throw new InvalidOperationException("Jednostka surowca jest wymagana.");

		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		if (surowiec.Id == 0)
		{
			command.CommandText = """
			                      INSERT INTO magazyn.surowce (nazwa_surowca, typ, jednostka)
			                      VALUES (?, ?, ?)
			                      RETURNING id_surowca;
			                      """;
			command.Parameters.Add(new OdbcParameter("@Nazwa", surowiec.Nazwa.Trim()));
			command.Parameters.Add(new OdbcParameter("@Typ", surowiec.Typ.Trim()));
			command.Parameters.Add(new OdbcParameter("@Jednostka", surowiec.Jednostka.Trim()));
			return Convert.ToInt32(command.ExecuteScalar());
		}

		command.CommandText = """
		                      UPDATE magazyn.surowce
		                      SET nazwa_surowca = ?, typ = ?, jednostka = ?
		                      WHERE id_surowca = ?;
		                      """;
		command.Parameters.Add(new OdbcParameter("@Nazwa", surowiec.Nazwa.Trim()));
		command.Parameters.Add(new OdbcParameter("@Typ", surowiec.Typ.Trim()));
		command.Parameters.Add(new OdbcParameter("@Jednostka", surowiec.Jednostka.Trim()));
		command.Parameters.Add(new OdbcParameter("@IdSurowca", surowiec.Id));
		command.ExecuteNonQuery();
		return surowiec.Id;
	}

	public void AddOrUpdateRawBatch(PartiaSurowca batch)
	{
		if (batch.IdSurowca <= 0) throw new InvalidOperationException("Wybierz surowiec dla partii.");
		if (string.IsNullOrWhiteSpace(batch.NumerPartii)) throw new InvalidOperationException("Numer partii jest wymagany.");
		if (batch.IloscDostepna < 0) throw new InvalidOperationException("Ilość partii nie może być ujemna.");

		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		if (batch.Id == 0)
		{
			command.CommandText = """
			                      INSERT INTO magazyn.partie_surowcow
			                          (numer_partii, data_waznosci, ilosc_dostepna, ilosc_zarezerwowana, id_surowca_surowce)
			                      VALUES (?, ?, ?, 0, ?)
			                      ON CONFLICT (numer_partii, id_surowca_surowce)
			                      DO UPDATE SET
			                          data_waznosci = EXCLUDED.data_waznosci,
			                          ilosc_dostepna = magazyn.partie_surowcow.ilosc_dostepna + EXCLUDED.ilosc_dostepna
			                      RETURNING id_partii_surowca;
			                      """;
			command.Parameters.Add(new OdbcParameter("@NumerPartii", batch.NumerPartii.Trim()));
			command.Parameters.Add(new OdbcParameter("@DataWaznosci", batch.DataWaznosci));
			command.Parameters.Add(new OdbcParameter("@Ilosc", batch.IloscDostepna));
			command.Parameters.Add(new OdbcParameter("@IdSurowca", batch.IdSurowca));
			batch.Id = Convert.ToInt32(command.ExecuteScalar());
			return;
		}

		command.CommandText = """
		                      UPDATE magazyn.partie_surowcow
		                      SET numer_partii = ?, data_waznosci = ?, ilosc_dostepna = ?
		                      WHERE id_partii_surowca = ? AND ? >= ilosc_zarezerwowana;
		                      """;
		command.Parameters.Add(new OdbcParameter("@NumerPartii", batch.NumerPartii.Trim()));
		command.Parameters.Add(new OdbcParameter("@DataWaznosci", batch.DataWaznosci));
		command.Parameters.Add(new OdbcParameter("@Ilosc", batch.IloscDostepna));
		command.Parameters.Add(new OdbcParameter("@IdPartii", batch.Id));
		command.Parameters.Add(new OdbcParameter("@IloscCheck", batch.IloscDostepna));
		if (command.ExecuteNonQuery() == 0)
			throw new InvalidOperationException("Stan partii nie może spaść poniżej rezerwacji.");
	}

	public IEnumerable<Receptura> GetRecipes()
	{
		var recipes = new Dictionary<int, Receptura>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT r.id_receptury, r.nazwa, r.opis, r.zatwierdzona, r.koszt_przygotowania,
		                             rs.id_surowca_surowce, rs.ilosc,
		                             s.nazwa_surowca, s.jednostka
		                      FROM apteka.receptury r
		                      LEFT JOIN apteka.receptury_surowce rs ON rs.id_receptury_receptury = r.id_receptury
		                      LEFT JOIN magazyn.surowce s ON s.id_surowca = rs.id_surowca_surowce
		                      ORDER BY r.nazwa, s.nazwa_surowca;
		                      """;

		using var reader = command.ExecuteReader();
		while (reader.Read())
		{
			var id = Convert.ToInt32(reader["id_receptury"]);
			if (!recipes.TryGetValue(id, out var recipe))
			{
				recipe = new Receptura
				{
					Id = id,
					Nazwa = reader["nazwa"].ToString() ?? string.Empty,
					Opis = reader["opis"].ToString() ?? string.Empty,
					Zatwierdzona = OdbcValue.ToBoolean(reader["zatwierdzona"]),
					KosztPrzygotowania = Convert.ToDecimal(reader["koszt_przygotowania"])
				};
				recipes.Add(id, recipe);
			}

			if (reader["id_surowca_surowce"] == DBNull.Value) continue;
			recipe.Skladniki.Add(new RecepturaSkladnik
			{
				IdReceptury = id,
				IdSurowca = Convert.ToInt32(reader["id_surowca_surowce"]),
				NazwaSurowca = reader["nazwa_surowca"].ToString() ?? string.Empty,
				Jednostka = reader["jednostka"].ToString() ?? string.Empty,
				Ilosc = Convert.ToDecimal(reader["ilosc"])
			});
		}

		return recipes.Values;
	}

	public int AddOrUpdateRecipe(Receptura recipe)
	{
		if (string.IsNullOrWhiteSpace(recipe.Nazwa))
			throw new InvalidOperationException("Nazwa receptury jest wymagana.");
		if (recipe.KosztPrzygotowania < 0)
			throw new InvalidOperationException("Koszt przygotowania nie może być ujemny.");
		if (recipe.Skladniki.Count == 0)
			throw new InvalidOperationException("Receptura musi mieć przynajmniej jeden składnik.");

		using var connection = dbService.CreateConnection();
		using var transaction = connection.BeginTransaction();
		try
		{
			using var command = connection.CreateCommand();
			command.Transaction = transaction;

			if (recipe.Id == 0)
			{
				command.CommandText = """
				                      INSERT INTO apteka.receptury (nazwa, opis, zatwierdzona, koszt_przygotowania)
				                      VALUES (?, ?, ?, ?)
				                      RETURNING id_receptury;
				                      """;
				command.Parameters.Add(new OdbcParameter("@Nazwa", recipe.Nazwa.Trim()));
				command.Parameters.Add(new OdbcParameter("@Opis", recipe.Opis.Trim()));
				command.Parameters.Add(new OdbcParameter("@Zatwierdzona", recipe.Zatwierdzona));
				command.Parameters.Add(new OdbcParameter("@Koszt", recipe.KosztPrzygotowania));
				recipe.Id = Convert.ToInt32(command.ExecuteScalar());
				command.Parameters.Clear();
			}
			else
			{
				command.CommandText = """
				                      UPDATE apteka.receptury
				                      SET nazwa = ?, opis = ?, zatwierdzona = ?, koszt_przygotowania = ?
				                      WHERE id_receptury = ?;
				                      """;
				command.Parameters.Add(new OdbcParameter("@Nazwa", recipe.Nazwa.Trim()));
				command.Parameters.Add(new OdbcParameter("@Opis", recipe.Opis.Trim()));
				command.Parameters.Add(new OdbcParameter("@Zatwierdzona", recipe.Zatwierdzona));
				command.Parameters.Add(new OdbcParameter("@Koszt", recipe.KosztPrzygotowania));
				command.Parameters.Add(new OdbcParameter("@IdReceptury", recipe.Id));
				command.ExecuteNonQuery();
				command.Parameters.Clear();

				command.CommandText = "DELETE FROM apteka.receptury_surowce WHERE id_receptury_receptury = ?;";
				command.Parameters.Add(new OdbcParameter("@IdReceptury", recipe.Id));
				command.ExecuteNonQuery();
				command.Parameters.Clear();
			}

			foreach (var ingredient in recipe.Skladniki)
			{
				if (ingredient.IdSurowca <= 0 || ingredient.Ilosc <= 0)
					throw new InvalidOperationException("Każdy składnik receptury musi mieć surowiec i dodatnią ilość.");

				command.CommandText = """
				                      INSERT INTO apteka.receptury_surowce
				                          (id_receptury_receptury, id_surowca_surowce, ilosc)
				                      VALUES (?, ?, ?);
				                      """;
				command.Parameters.Add(new OdbcParameter("@IdReceptury", recipe.Id));
				command.Parameters.Add(new OdbcParameter("@IdSurowca", ingredient.IdSurowca));
				command.Parameters.Add(new OdbcParameter("@Ilosc", ingredient.Ilosc));
				command.ExecuteNonQuery();
				command.Parameters.Clear();
			}

			transaction.Commit();
			return recipe.Id;
		}
		catch
		{
			transaction.Rollback();
			throw;
		}
	}

	public void DeleteRecipe(int recipeId)
	{
		if (recipeId <= 0) return;
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "DELETE FROM apteka.receptury WHERE id_receptury = ?;";
		command.Parameters.Add(new OdbcParameter("@IdReceptury", recipeId));
		command.ExecuteNonQuery();
	}

	public int ExecuteRecipe(int recipeId, int? prescriptionId, int amount, string documentType)
	{
		if (recipeId <= 0) throw new InvalidOperationException("Wybierz recepturę do wykonania.");
		if (amount <= 0) throw new InvalidOperationException("Ilość wykonań musi być większa od zera.");

		using var connection = dbService.CreateConnection();
		using var transaction = connection.BeginTransaction();
		try
		{
			using var command = connection.CreateCommand();
			command.Transaction = transaction;

			var recipe = LoadRecipeForUpdate(command, recipeId);
			if (!recipe.Zatwierdzona)
				throw new InvalidOperationException("Receptura musi być zatwierdzona przed wykonaniem.");
			if (recipe.Skladniki.Count == 0)
				throw new InvalidOperationException("Receptura nie ma składników.");

			foreach (var ingredient in recipe.Skladniki)
				EnsureRawMaterialAvailability(command, ingredient.IdSurowca, ingredient.Ilosc * amount, ingredient.NazwaSurowca);

			var saleId = CreateSale(command, documentType);
			var saleLineId = CreateRecipeSaleLine(command, saleId, amount, recipe.KosztPrzygotowania);
			var executionId = CreateExecution(command, recipeId, prescriptionId, saleId, saleLineId, amount, recipe.KosztPrzygotowania);

			foreach (var ingredient in recipe.Skladniki)
				ConsumeRawMaterial(command, executionId, ingredient.IdSurowca, ingredient.Ilosc * amount);

			UpdateSaleTotal(command, saleId, recipe.KosztPrzygotowania * amount);

			if (prescriptionId.HasValue)
				MarkPrescriptionAsRealized(command, prescriptionId.Value, saleId);

			transaction.Commit();
			return executionId;
		}
		catch
		{
			transaction.Rollback();
			throw;
		}
	}

	public IEnumerable<WykonanieReceptury> GetExecutions()
	{
		var result = new List<WykonanieReceptury>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT w.id_wykonania, w.id_receptury_receptury, r.nazwa,
		                             w.id_recepty_recepta, w.id_sprzedazy_sprzedaze,
		                             w.data_wykonania, w.ilosc, w.koszt_jednostkowy
		                      FROM apteka.wykonania_receptur w
		                      JOIN apteka.receptury r ON r.id_receptury = w.id_receptury_receptury
		                      ORDER BY w.data_wykonania DESC;
		                      """;

		using var reader = command.ExecuteReader();
		while (reader.Read())
			result.Add(new WykonanieReceptury
			{
				Id = Convert.ToInt32(reader["id_wykonania"]),
				IdReceptury = Convert.ToInt32(reader["id_receptury_receptury"]),
				NazwaReceptury = reader["nazwa"].ToString() ?? string.Empty,
				IdRecepty = reader["id_recepty_recepta"] == DBNull.Value ? null : Convert.ToInt32(reader["id_recepty_recepta"]),
				IdSprzedazy = Convert.ToInt32(reader["id_sprzedazy_sprzedaze"]),
				DataWykonania = Convert.ToDateTime(reader["data_wykonania"]),
				Ilosc = Convert.ToInt32(reader["ilosc"]),
				KosztJednostkowy = Convert.ToDecimal(reader["koszt_jednostkowy"])
			});

		return result;
	}

	private static Receptura LoadRecipeForUpdate(IDbCommand command, int recipeId)
	{
		var recipe = new Receptura();
		command.CommandText = """
		                      SELECT r.id_receptury, r.nazwa, r.opis, r.zatwierdzona, r.koszt_przygotowania,
		                             rs.id_surowca_surowce, rs.ilosc,
		                             s.nazwa_surowca, s.jednostka
		                      FROM apteka.receptury r
		                      LEFT JOIN apteka.receptury_surowce rs ON rs.id_receptury_receptury = r.id_receptury
		                      LEFT JOIN magazyn.surowce s ON s.id_surowca = rs.id_surowca_surowce
		                      WHERE r.id_receptury = ?;
		                      """;
		command.Parameters.Add(new OdbcParameter("@IdReceptury", recipeId));
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
			{
				if (recipe.Id == 0)
				{
					recipe.Id = Convert.ToInt32(reader["id_receptury"]);
					recipe.Nazwa = reader["nazwa"].ToString() ?? string.Empty;
					recipe.Opis = reader["opis"].ToString() ?? string.Empty;
					recipe.Zatwierdzona = OdbcValue.ToBoolean(reader["zatwierdzona"]);
					recipe.KosztPrzygotowania = Convert.ToDecimal(reader["koszt_przygotowania"]);
				}

				if (reader["id_surowca_surowce"] == DBNull.Value) continue;
				recipe.Skladniki.Add(new RecepturaSkladnik
				{
					IdReceptury = recipe.Id,
					IdSurowca = Convert.ToInt32(reader["id_surowca_surowce"]),
					NazwaSurowca = reader["nazwa_surowca"].ToString() ?? string.Empty,
					Jednostka = reader["jednostka"].ToString() ?? string.Empty,
					Ilosc = Convert.ToDecimal(reader["ilosc"])
				});
			}
		}

		command.Parameters.Clear();
		if (recipe.Id == 0) throw new InvalidOperationException("Nie znaleziono receptury.");
		return recipe;
	}

	private static void EnsureRawMaterialAvailability(IDbCommand command, int rawMaterialId, decimal requiredQuantity, string name)
	{
			command.CommandText = """
			                      SELECT COALESCE(SUM(ilosc_dostepna - ilosc_zarezerwowana), 0)
			                      FROM magazyn.partie_surowcow
			                      WHERE id_surowca_surowce = ?
			                        AND data_waznosci >= CURRENT_DATE;
			                      """;
		command.Parameters.Add(new OdbcParameter("@IdSurowca", rawMaterialId));
		var available = Convert.ToDecimal(command.ExecuteScalar());
		command.Parameters.Clear();
		if (available < requiredQuantity)
			throw new InvalidOperationException($"Brak surowca {name}. Wymagane: {requiredQuantity}, dostępne: {available}.");
	}

	private static int CreateSale(IDbCommand command, string documentType)
	{
		command.CommandText = """
		                      INSERT INTO apteka.sprzedaze (typ_dokumentu, data_sprzedazy)
		                      VALUES (?, NOW())
		                      RETURNING id_sprzedazy;
		                      """;
		command.Parameters.Add(new OdbcParameter("@TypDokumentu", documentType == "Faktura" ? "Faktura" : "Paragon"));
		var saleId = Convert.ToInt32(command.ExecuteScalar());
		command.Parameters.Clear();
		return saleId;
	}

	private static int CreateRecipeSaleLine(IDbCommand command, int saleId, int amount, decimal price)
	{
		command.CommandText = """
		                      INSERT INTO apteka.pozycja_sprzedazy
		                          (ilosc, cena_jednostkowa, typ_produktu, id_sprzedazy_sprzedaze)
		                      VALUES (?, ?, 'Receptura', ?)
		                      RETURNING id_pozycji;
		                      """;
		command.Parameters.Add(new OdbcParameter("@Ilosc", amount));
		command.Parameters.Add(new OdbcParameter("@Cena", price));
		command.Parameters.Add(new OdbcParameter("@IdSprzedazy", saleId));
		var saleLineId = Convert.ToInt32(command.ExecuteScalar());
		command.Parameters.Clear();
		return saleLineId;
	}

	private static int CreateExecution(IDbCommand command, int recipeId, int? prescriptionId, int saleId, int saleLineId,
		int amount, decimal price)
	{
		command.CommandText = """
		                      INSERT INTO apteka.wykonania_receptur
		                          (id_receptury_receptury, id_recepty_recepta, id_sprzedazy_sprzedaze,
		                           id_pozycji_pozycja_sprzedazy, ilosc, koszt_jednostkowy)
		                      VALUES (?, ?, ?, ?, ?, ?)
		                      RETURNING id_wykonania;
		                      """;
		command.Parameters.Add(new OdbcParameter("@IdReceptury", recipeId));
		command.Parameters.Add(new OdbcParameter("@IdRecepty", prescriptionId.HasValue ? prescriptionId.Value : DBNull.Value));
		command.Parameters.Add(new OdbcParameter("@IdSprzedazy", saleId));
		command.Parameters.Add(new OdbcParameter("@IdPozycji", saleLineId));
		command.Parameters.Add(new OdbcParameter("@Ilosc", amount));
		command.Parameters.Add(new OdbcParameter("@Koszt", price));
		var executionId = Convert.ToInt32(command.ExecuteScalar());
		command.Parameters.Clear();
		return executionId;
	}

	private static void ConsumeRawMaterial(IDbCommand command, int executionId, int rawMaterialId, decimal requiredQuantity)
	{
		var batches = new List<(int Id, decimal Available)>();
			command.CommandText = """
			                      SELECT id_partii_surowca, ilosc_dostepna - ilosc_zarezerwowana AS ilosc_do_uzycia
			                      FROM magazyn.partie_surowcow
			                      WHERE id_surowca_surowce = ?
			                        AND data_waznosci >= CURRENT_DATE
			                        AND (ilosc_dostepna - ilosc_zarezerwowana) > 0
			                      ORDER BY data_waznosci, id_partii_surowca;
			                      """;
		command.Parameters.Add(new OdbcParameter("@IdSurowca", rawMaterialId));
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
				batches.Add((Convert.ToInt32(reader["id_partii_surowca"]),
					Convert.ToDecimal(reader["ilosc_do_uzycia"])));
		}

		command.Parameters.Clear();

		var remaining = requiredQuantity;
		foreach (var (batchId, available) in batches)
		{
			if (remaining <= 0) break;
			var consumed = Math.Min(remaining, available);

			command.CommandText = """
			                      UPDATE magazyn.partie_surowcow
			                      SET ilosc_dostepna = ilosc_dostepna - ?
			                      WHERE id_partii_surowca = ?
			                        AND (ilosc_dostepna - ilosc_zarezerwowana) >= ?
			                      RETURNING ilosc_dostepna;
			                      """;
			command.Parameters.Add(new OdbcParameter("@Ilosc", consumed));
			command.Parameters.Add(new OdbcParameter("@IdPartii", batchId));
			command.Parameters.Add(new OdbcParameter("@IloscCheck", consumed));
			var updated = command.ExecuteScalar();
			command.Parameters.Clear();
			if (updated is null || updated == DBNull.Value)
				throw new InvalidOperationException("Stan surowca zmienił się w trakcie wykonania receptury.");

			command.CommandText = """
			                      INSERT INTO apteka.surowce_w_wykonaniu
			                          (id_wykonania_wykonania_receptur, id_partii_surowca_partie_surowcow, ilosc)
			                      VALUES (?, ?, ?);
			                      """;
			command.Parameters.Add(new OdbcParameter("@IdWykonania", executionId));
			command.Parameters.Add(new OdbcParameter("@IdPartii", batchId));
			command.Parameters.Add(new OdbcParameter("@Ilosc", consumed));
			command.ExecuteNonQuery();
			command.Parameters.Clear();

			remaining -= consumed;
		}

		if (remaining > 0)
			throw new InvalidOperationException("Nie udało się pobrać pełnej ilości surowca.");
	}

	private static void UpdateSaleTotal(IDbCommand command, int saleId, decimal total)
	{
		command.CommandText = "UPDATE apteka.sprzedaze SET kwota = ? WHERE id_sprzedazy = ?;";
		command.Parameters.Add(new OdbcParameter("@Kwota", total));
		command.Parameters.Add(new OdbcParameter("@IdSprzedazy", saleId));
		command.ExecuteNonQuery();
		command.Parameters.Clear();
	}

	private static void MarkPrescriptionAsRealized(IDbCommand command, int prescriptionId, int saleId)
	{
		command.CommandText = """
		                      UPDATE apteka.recepta
		                      SET id_sprzedazy_sprzedaze = ?, data_realizacji = NOW()
		                      WHERE id_recepty = ?;
		                      """;
		command.Parameters.Add(new OdbcParameter("@IdSprzedazy", saleId));
		command.Parameters.Add(new OdbcParameter("@IdRecepty", prescriptionId));
		command.ExecuteNonQuery();
		command.Parameters.Clear();
	}
}
