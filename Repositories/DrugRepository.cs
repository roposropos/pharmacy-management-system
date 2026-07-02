using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using Apteka.Models;

namespace Apteka.Repositories;

public class DrugRepository(DatabaseService dbService, AddressRepository addressRepository)
{
	private const string DefaultQuery = """
	                                    SELECT
	                                        l.id_leku,
	                                        l.nazwa, l.bez_recepty, l.substancja_czynna, l.id_producenta_producenci,
	                                        p.nazwa AS nazwa_producenta, p.id_adresu_adresy,
	                                        w.id_wariantu,
	                                        w.kod_ean, w.postac, w.dawkowanie, w.ilosc AS ilosc_w_opakowaniu,
	                                        pl.id_partii, pl.numer_partii, pl.data_waznosci, pl.ilosc_dostepna, pl.ilosc_zarezerwowana
	                                    FROM apteka.leki l
	                                             LEFT JOIN apteka.producenci p ON l.id_producenta_producenci = p.id_producenta
	                                             LEFT JOIN apteka.warianty_lekow w ON l.id_leku = w.id_leku_leki
	                                             LEFT JOIN magazyn.partie_lekow pl ON w.id_wariantu = pl.id_wariantu_warianty_lekow
	                                    """;

	public IEnumerable<Lek> GetAll()
	{
		var lookup = new Dictionary<int, Lek>();
		var variantLookup = new Dictionary<int, WariantLeku>();
		using var connection = dbService.CreateConnection();
		using (var command = connection.CreateCommand())
		{
				command.CommandText = DefaultQuery + "\nORDER BY l.nazwa, w.dawkowanie, pl.data_waznosci;";

			using (var reader = command.ExecuteReader())
			{
				while (reader.Read())
					ExtractDrugDetails(reader, lookup, variantLookup);
			}
		}

		foreach (var lek in lookup.Values)
			if (addressRepository.GetAdresById(lek.Producent.IdAdresu) is { } adres)
				lek.Producent.Adres = adres;

		return lookup.Values;
	}

	public IEnumerable<Producent> GetProducers()
	{
		var producers = new List<Producent>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT p.id_producenta, p.nazwa, p.id_adresu_adresy,
		                             a.ulica, a.nr_domu, a.nr_lokalu, a.kod_pocztowy, a.miasto, a.kraj
		                      FROM apteka.producenci p
		                      JOIN apteka.adresy a ON a.id_adresu = p.id_adresu_adresy
		                      ORDER BY p.nazwa;
		                      """;

		using var reader = command.ExecuteReader();
		while (reader.Read())
		{
			var idAdresu = Convert.ToInt32(reader["id_adresu_adresy"]);
			producers.Add(new Producent
			{
				Id = Convert.ToInt32(reader["id_producenta"]),
				Nazwa = reader["nazwa"].ToString() ?? string.Empty,
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

		return producers;
	}

	public int AddOrUpdate(Lek lek)
	{
		ValidateDrug(lek);
		var producerId = lek.IdProducenta > 0 ? lek.IdProducenta : lek.Producent.Id;
		if (producerId <= 0) throw new InvalidOperationException("Wybierz producenta leku.");

		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();

		if (lek.Id == 0)
		{
			command.CommandText = """
			                      INSERT INTO apteka.leki
			                          (nazwa, bez_recepty, substancja_czynna, id_producenta_producenci)
			                      VALUES (?, ?, ?, ?)
			                      RETURNING id_leku;
			                      """;
			command.Parameters.Add(new OdbcParameter("@Nazwa", lek.Nazwa.Trim()));
			command.Parameters.Add(new OdbcParameter("@BezRecepty", lek.BezRecepty));
			command.Parameters.Add(new OdbcParameter("@SubstancjaCzynna", lek.SubstancjaCzynna.Trim()));
			command.Parameters.Add(new OdbcParameter("@IdProducenta", producerId));
			return Convert.ToInt32(command.ExecuteScalar());
		}

		command.CommandText = """
		                      UPDATE apteka.leki
		                      SET nazwa = ?, bez_recepty = ?, substancja_czynna = ?, id_producenta_producenci = ?
		                      WHERE id_leku = ?;
		                      """;
		command.Parameters.Add(new OdbcParameter("@Nazwa", lek.Nazwa.Trim()));
		command.Parameters.Add(new OdbcParameter("@BezRecepty", lek.BezRecepty));
		command.Parameters.Add(new OdbcParameter("@SubstancjaCzynna", lek.SubstancjaCzynna.Trim()));
		command.Parameters.Add(new OdbcParameter("@IdProducenta", producerId));
		command.Parameters.Add(new OdbcParameter("@IdLeku", lek.Id));
		command.ExecuteNonQuery();
		return lek.Id;
	}

	public void Delete(int drugId)
	{
		if (drugId <= 0) return;
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "DELETE FROM apteka.leki WHERE id_leku = ?;";
		command.Parameters.Add(new OdbcParameter("@IdLeku", drugId));
		command.ExecuteNonQuery();
	}

	public int AddOrUpdateVariant(int drugId, WariantLeku variant)
	{
		if (drugId <= 0) throw new InvalidOperationException("Najpierw zapisz lek, a potem dodaj wariant.");
		ValidateVariant(variant);

		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		if (variant.Id == 0)
		{
			command.CommandText = """
			                      INSERT INTO apteka.warianty_lekow
			                          (kod_ean, postac, dawkowanie, ilosc, id_leku_leki)
			                      VALUES (?, ?, ?, ?, ?)
			                      RETURNING id_wariantu;
			                      """;
			command.Parameters.Add(new OdbcParameter("@KodEan", variant.KodEan));
			command.Parameters.Add(new OdbcParameter("@Postac", (short)variant.Postac));
			command.Parameters.Add(new OdbcParameter("@Dawkowanie", variant.Dawka.Trim()));
			command.Parameters.Add(new OdbcParameter("@Ilosc", variant.Ilosc));
			command.Parameters.Add(new OdbcParameter("@IdLeku", drugId));
			return Convert.ToInt32(command.ExecuteScalar());
		}

		command.CommandText = """
		                      UPDATE apteka.warianty_lekow
		                      SET kod_ean = ?, postac = ?, dawkowanie = ?, ilosc = ?
		                      WHERE id_wariantu = ?;
		                      """;
		command.Parameters.Add(new OdbcParameter("@KodEan", variant.KodEan));
		command.Parameters.Add(new OdbcParameter("@Postac", (short)variant.Postac));
		command.Parameters.Add(new OdbcParameter("@Dawkowanie", variant.Dawka.Trim()));
		command.Parameters.Add(new OdbcParameter("@Ilosc", variant.Ilosc));
		command.Parameters.Add(new OdbcParameter("@IdWariantu", variant.Id));
		command.ExecuteNonQuery();
		return variant.Id;
	}

	public void DeleteVariant(int variantId)
	{
		if (variantId <= 0) return;
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "DELETE FROM apteka.warianty_lekow WHERE id_wariantu = ?;";
		command.Parameters.Add(new OdbcParameter("@IdWariantu", variantId));
		command.ExecuteNonQuery();
	}

	private static void ExtractDrugDetails(IDataReader reader, Dictionary<int, Lek> drugLookup,
		Dictionary<int, WariantLeku> variantLookup)
	{
		var idLeku = Convert.ToInt32(reader["id_leku"]);

		if (!drugLookup.TryGetValue(idLeku, out var currentLek))
		{
			currentLek = new Lek
			{
				Id = idLeku,
				Nazwa = reader["nazwa"].ToString() ?? string.Empty,
				BezRecepty = OdbcValue.ToBoolean(reader["bez_recepty"]),
				SubstancjaCzynna = reader["substancja_czynna"].ToString() ?? string.Empty,
				IdProducenta = Convert.ToInt32(reader["id_producenta_producenci"]),
				Producent = new Producent
				{
					Id = Convert.ToInt32(reader["id_producenta_producenci"]),
					Nazwa = reader["nazwa_producenta"].ToString() ?? string.Empty,
					IdAdresu = Convert.ToInt32(reader["id_adresu_adresy"])
				},
				Warianty = new List<WariantLeku>()
			};
			drugLookup.Add(idLeku, currentLek);
		}

		if (reader.IsDBNull(7)) return;
		var idWariantu = Convert.ToInt32(reader["id_wariantu"]);
		if (!variantLookup.TryGetValue(idWariantu, out var wariant))
		{
			wariant = new WariantLeku
			{
				Id = Convert.ToInt32(reader["id_wariantu"]),
				KodEan = Convert.ToInt64(reader["kod_ean"]),
				Postac = (PostacLeku)Convert.ToInt16(reader["postac"]),
				Dawka = reader["dawkowanie"].ToString() ?? string.Empty,
				Ilosc = Convert.ToInt32(reader["ilosc_w_opakowaniu"])
			};
			currentLek.Warianty.Add(wariant);
			variantLookup.Add(idWariantu, wariant);
		}

		if (reader.IsDBNull(13)) return;
		wariant.PartieLekow.Add(new PartiaLeku
		{
			Id = Convert.ToInt32(reader["id_partii"]),
			NumerPartii = reader["numer_partii"].ToString() ?? string.Empty,
			DataWaznosci = Convert.ToDateTime(reader["data_waznosci"]),
			IloscDostepna = Convert.ToInt32(reader["ilosc_dostepna"]),
			IloscZarezerwowana = Convert.ToInt32(reader["ilosc_zarezerwowana"])
		});
	}

	public IEnumerable<Lek> GetNoPrescription()
	{
		var drugLookup = new Dictionary<int, Lek>();
		var variantLookup = new Dictionary<int, WariantLeku>();

		using var connection = dbService.CreateConnection();
		using (var command = connection.CreateCommand())
		{
				command.CommandText = DefaultQuery +
				                      """
				                      
				                      WHERE l.bez_recepty = true
				                        AND pl.data_waznosci >= CURRENT_DATE
				                        AND (pl.ilosc_dostepna - pl.ilosc_zarezerwowana) > 0
				                      ORDER BY l.nazwa, w.dawkowanie, pl.data_waznosci
				                      """;

			using (var reader = command.ExecuteReader())
			{
				while (reader.Read()) ExtractDrugDetails(reader, drugLookup, variantLookup);
			}
		}

		foreach (var lek in drugLookup.Values)
			if (addressRepository.GetAdresById(lek.Producent.IdAdresu) is { } adres)
				lek.Producent.Adres = adres;

		return drugLookup.Values;
	}

	public IEnumerable<Lek> GetFromPrescription(int receptaId)
	{
		if (receptaId <= 0) return [];
		var drugLookup = new Dictionary<int, Lek>();
		var variantLookup = new Dictionary<int, WariantLeku>();

		using var connection = dbService.CreateConnection();
		using (var command = connection.CreateCommand())
		{
				command.CommandText = DefaultQuery + """

				                                     JOIN apteka.leki_w_recepcie lr ON lr.id_wariantu_warianty_lekow = w.id_wariantu 
				                                     WHERE lr.id_recepty_recepta = ?
				                                       AND pl.data_waznosci >= CURRENT_DATE
				                                       AND (pl.ilosc_dostepna - pl.ilosc_zarezerwowana) > 0
				                                     ORDER BY l.nazwa, w.dawkowanie, pl.data_waznosci
				                                     """;
			command.Parameters.Add(new OdbcParameter("@IdRecepty", receptaId));
			using (var reader = command.ExecuteReader())
			{
				while (reader.Read()) ExtractDrugDetails(reader, drugLookup, variantLookup);
			}
		}

		foreach (var lek in drugLookup.Values)
			if (addressRepository.GetAdresById(lek.Producent.IdAdresu) is { } adres)
				lek.Producent.Adres = adres;

		return drugLookup.Values;
	}

	public IEnumerable<Lek> GetAlternatives(string substancjaCzynna)
	{
		if (string.IsNullOrWhiteSpace(substancjaCzynna)) return [];
		var drugLookup = new Dictionary<int, Lek>();
		var variantLookup = new Dictionary<int, WariantLeku>();

		using var connection = dbService.CreateConnection();
		using (var command = connection.CreateCommand())
		{
			command.CommandText = DefaultQuery + "\nWHERE l.substancja_czynna = ?";
			command.Parameters.Add(new OdbcParameter("@substancjaCzynna", substancjaCzynna));
			using (var reader = command.ExecuteReader())
			{
				while (reader.Read()) ExtractDrugDetails(reader, drugLookup, variantLookup);
			}
		}

		foreach (var lek in drugLookup.Values)
			if (addressRepository.GetAdresById(lek.Producent.IdAdresu) is { } adres)
				lek.Producent.Adres = adres;

		return drugLookup.Values;
	}

	private static void ValidateDrug(Lek lek)
	{
		if (string.IsNullOrWhiteSpace(lek.Nazwa))
			throw new InvalidOperationException("Nazwa leku jest wymagana.");
		if (string.IsNullOrWhiteSpace(lek.SubstancjaCzynna))
			throw new InvalidOperationException("Substancja czynna jest wymagana.");
	}

	private static void ValidateVariant(WariantLeku variant)
	{
		if (variant.KodEan <= 0)
			throw new InvalidOperationException("Kod EAN wariantu jest wymagany.");
		if (string.IsNullOrWhiteSpace(variant.Dawka))
			throw new InvalidOperationException("Dawka wariantu jest wymagana.");
		if (variant.Ilosc <= 0)
			throw new InvalidOperationException("Ilość w opakowaniu musi być większa od zera.");
	}
}
