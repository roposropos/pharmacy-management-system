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
	                                             JOIN apteka.warianty_lekow w ON l.id_leku = w.id_leku_leki
	                                             LEFT JOIN apteka.producenci p ON l.id_producenta_producenci = p.id_producenta
	                                             LEFT JOIN magazyn.partie_lekow pl ON w.id_wariantu = pl.id_wariantu_warianty_lekow
	                                    """;

	public IEnumerable<Lek> GetAll()
	{
		var lookup = new Dictionary<int, Lek>();
		var variantLookup = new Dictionary<int, WariantLeku>();
		using var connection = dbService.CreateConnection();
		using (var command = connection.CreateCommand())
		{
			command.CommandText = """
			                      SELECT
			                          l.id_leku,
			                          l.nazwa, l.bez_recepty, l.substancja_czynna, l.id_producenta_producenci,
			                          p.nazwa AS nazwa_producenta, p.id_adresu_adresy,
			                          w.id_wariantu,
			                          w.kod_ean, w.postac, w.dawkowanie, w.ilosc AS ilosc_w_opakowaniu,
			                          pl.id_partii, pl.numer_partii, pl.data_waznosci, pl.ilosc_dostepna, pl.ilosc_zarezerwowana
			                      FROM apteka.leki l
			                               JOIN apteka.warianty_lekow w ON l.id_leku = w.id_leku_leki
			                               LEFT JOIN apteka.producenci p ON l.id_producenta_producenci = p.id_producenta
			                               LEFT JOIN magazyn.partie_lekow pl ON w.id_wariantu = pl.id_wariantu_warianty_lekow
			                      """;

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
				BezRecepty = Convert.ToBoolean(Convert.ToInt32(reader["bez_recepty"])),
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
			                      "\nWHERE l.bez_recepty = true AND (pl.ilosc_dostepna - pl.ilosc_zarezerwowana) > 0";

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
			try
			{
				// command.CommandText = DefaultQuery + """
				//                                      JOIN apteka.leki_w_recepcie lr ON lr.id_wariantu_warianty_lekow = w.id_wariantu
				//                                      WHERE lr.id_recepty_recepta = ?";
				//                                      """;
				command.CommandText = DefaultQuery + """

				                                     JOIN apteka.leki_w_recepcie lr ON lr.id_wariantu_warianty_lekow = w.id_wariantu 
				                                     WHERE lr.id_recepty_recepta = ?
				                                     """;
				command.Parameters.Add(new OdbcParameter("@IdRecepty", receptaId));
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read()) ExtractDrugDetails(reader, drugLookup, variantLookup);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
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
}