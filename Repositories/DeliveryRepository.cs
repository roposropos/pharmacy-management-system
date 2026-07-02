using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using Apteka.Models;

namespace Apteka.Repositories;

public class DeliveryRepository(DatabaseService dbService, AddressRepository addressRepository)
{
	public IEnumerable<Dostawa> GetAll()
	{
		var deliveries = new List<Dostawa>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """"
		                      		SELECT d.id_dostawy, d.data_dostawy, d.id_dostawcy_dostawcy,
		                      		       dos.nazwa, dos."NIP", dos.id_adresu_adresy
		                      		FROM magazyn.dostawy d
		                      		JOIN magazyn.dostawcy dos ON dos.id_dostawcy = d.id_dostawcy_dostawcy
		                      		ORDER BY data_dostawy DESC
		                      """";

		using var reader = command.ExecuteReader();
		while (reader.Read())
			deliveries.Add(new Dostawa
			{
				Id = Convert.ToInt32(reader["id_dostawy"]),
				DataDostawy = Convert.ToDateTime(reader["data_dostawy"]),
				IdDostawcy = Convert.ToInt32(reader["id_dostawcy_dostawcy"]),
				Dostawca = new Dostawca
				{
					Id = Convert.ToInt32(reader["id_dostawcy_dostawcy"]),
					Nazwa = reader["nazwa"].ToString() ?? string.Empty,
					NIP = reader["NIP"].ToString() ?? string.Empty,
					IdAdresu = Convert.ToInt32(reader["id_adresu_adresy"])
				}
			});

		foreach (var dostawa in deliveries)
		{
			var adres = addressRepository.GetAdresById(dostawa.Dostawca!.IdAdresu);
			dostawa.Dostawca.Adres = adres;
		}

		return deliveries;
	}

	public IEnumerable<PozycjaDostawy> GetLines(int deliveryId)
	{
		if (deliveryId <= 0) return [];
		var lines = new List<PozycjaDostawy>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT pd.id_pozycji_dostawy,
		                             pd.id_dostawy_dostawy,
		                             pd.id_partii_partie_lekow,
		                             pd.id_partii_surowca_partie_surowcow,
		                             pd.ilosc,
		                             pd.cena_zakupu,
		                             COALESCE(pl.numer_partii, ps.numer_partii) AS numer_partii,
		                             COALESCE(pl.data_waznosci, ps.data_waznosci) AS data_waznosci,
		                             w.id_wariantu,
		                             s.id_surowca,
		                             CASE
		                                 WHEN pd.id_partii_partie_lekow IS NOT NULL
		                                 THEN l.nazwa || ' ' || w.dawkowanie || ' x' || w.ilosc
		                                 ELSE s.nazwa_surowca || ' (' || s.jednostka || ')'
		                             END AS nazwa,
		                             CASE
		                                 WHEN pd.id_partii_partie_lekow IS NOT NULL THEN 'Lek'
		                                 ELSE 'Surowiec'
		                             END AS typ_produktu
		                      FROM magazyn.pozycje_dostaw pd
		                      LEFT JOIN magazyn.partie_lekow pl ON pl.id_partii = pd.id_partii_partie_lekow
		                      LEFT JOIN apteka.warianty_lekow w ON w.id_wariantu = pl.id_wariantu_warianty_lekow
		                      LEFT JOIN apteka.leki l ON l.id_leku = w.id_leku_leki
		                      LEFT JOIN magazyn.partie_surowcow ps ON ps.id_partii_surowca = pd.id_partii_surowca_partie_surowcow
		                      LEFT JOIN magazyn.surowce s ON s.id_surowca = ps.id_surowca_surowce
		                      WHERE pd.id_dostawy_dostawy = ?
		                      ORDER BY pd.id_pozycji_dostawy;
		                      """;
		command.Parameters.Add(new OdbcParameter("@IdDostawy", deliveryId));

		using var reader = command.ExecuteReader();
		while (reader.Read())
			lines.Add(new PozycjaDostawy
			{
				Id = Convert.ToInt32(reader["id_pozycji_dostawy"]),
				IdDostawy = Convert.ToInt32(reader["id_dostawy_dostawy"]),
				IdPartii = reader["id_partii_partie_lekow"] == DBNull.Value
					? null
					: Convert.ToInt32(reader["id_partii_partie_lekow"]),
				IdPartiiSurowca = reader["id_partii_surowca_partie_surowcow"] == DBNull.Value
					? null
					: Convert.ToInt32(reader["id_partii_surowca_partie_surowcow"]),
				IdWariantu = reader["id_wariantu"] == DBNull.Value ? null : Convert.ToInt32(reader["id_wariantu"]),
				IdSurowca = reader["id_surowca"] == DBNull.Value ? null : Convert.ToInt32(reader["id_surowca"]),
				TypProduktu = reader["typ_produktu"].ToString() ?? string.Empty,
				Nazwa = reader["nazwa"].ToString() ?? string.Empty,
				NumerPartii = reader["numer_partii"].ToString() ?? string.Empty,
				DataWaznosci = Convert.ToDateTime(reader["data_waznosci"]),
				Ilosc = Convert.ToDecimal(reader["ilosc"]),
				CenaZakupu = Convert.ToDecimal(reader["cena_zakupu"])
			});

		return lines;
	}

	public IEnumerable<Surowiec> GetRawMaterials()
	{
		var result = new List<Surowiec>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT id_surowca, nazwa_surowca, typ, jednostka
		                      FROM magazyn.surowce
		                      ORDER BY nazwa_surowca;
		                      """;

		using var reader = command.ExecuteReader();
		while (reader.Read())
			result.Add(new Surowiec
			{
				Id = Convert.ToInt32(reader["id_surowca"]),
				Nazwa = reader["nazwa_surowca"].ToString() ?? string.Empty,
				Typ = reader["typ"].ToString() ?? string.Empty,
				Jednostka = reader["jednostka"].ToString() ?? string.Empty
			});

		return result;
	}

	public void Add(Dostawa dostawa)
	{
		Add(dostawa, []);
	}

	public void Add(Dostawa dostawa, IEnumerable<PozycjaDostawy> pozycje)
	{
		using var connection = dbService.CreateConnection();
		using var transaction = connection.BeginTransaction();
		try
		{
			using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = """
			                      INSERT INTO magazyn.dostawy (data_dostawy, id_dostawcy_dostawcy) 
			                      VALUES (?, ?) 
			                      RETURNING id_dostawy
			                      """;
			command.Parameters.Add(new OdbcParameter("@Data", dostawa.DataDostawy));
			command.Parameters.Add(new OdbcParameter("@IdDostawcy", dostawa.IdDostawcy));

			dostawa.Id = Convert.ToInt32(command.ExecuteScalar());
			command.Parameters.Clear();

				foreach (var pozycja in pozycje)
				{
					if (pozycja.IdWariantu.HasValue)
						SaveDrugDeliveryLine(command, dostawa.Id, pozycja);
					else if (pozycja.IdSurowca.HasValue)
						SaveRawMaterialDeliveryLine(command, dostawa.Id, pozycja);
					else
						throw new InvalidOperationException("Pozycja dostawy musi wskazywać lek albo surowiec.");
				}

			transaction.Commit();
		}
		catch
		{
			transaction.Rollback();
			throw;
			}
	}

	public void Delete(int deliveryId)
	{
		if (deliveryId <= 0) return;
		using var connection = dbService.CreateConnection();
		using var transaction = connection.BeginTransaction();
		try
		{
			using var command = connection.CreateCommand();
			command.Transaction = transaction;

			foreach (var line in LoadLinesForDelete(command, deliveryId))
			{
				if (line.IdPartii.HasValue)
					ReverseDrugStock(command, line.IdPartii.Value, ToWholeQuantity(line.Ilosc));
				else if (line.IdPartiiSurowca.HasValue)
					ReverseRawMaterialStock(command, line.IdPartiiSurowca.Value, line.Ilosc);
			}

			command.CommandText = "DELETE FROM magazyn.dostawy WHERE id_dostawy = ?;";
			command.Parameters.Add(new OdbcParameter("@IdDostawy", deliveryId));
			command.ExecuteNonQuery();
			command.Parameters.Clear();

			transaction.Commit();
		}
		catch
		{
			transaction.Rollback();
			throw;
		}
	}

	private static void SaveDrugDeliveryLine(IDbCommand command, int deliveryId, PozycjaDostawy line)
	{
		var quantity = ToWholeQuantity(line.Ilosc);
		command.CommandText = """
		                      INSERT INTO magazyn.partie_lekow
		                          (numer_partii, data_waznosci, ilosc_dostepna, ilosc_zarezerwowana, id_wariantu_warianty_lekow)
		                      VALUES (?, ?, ?, 0, ?)
		                      ON CONFLICT (numer_partii, id_wariantu_warianty_lekow)
		                      DO UPDATE SET
		                          data_waznosci = EXCLUDED.data_waznosci,
		                          ilosc_dostepna = magazyn.partie_lekow.ilosc_dostepna + EXCLUDED.ilosc_dostepna
		                      RETURNING id_partii;
		                      """;
		command.Parameters.Add(new OdbcParameter("@NumerPartii", line.NumerPartii));
		command.Parameters.Add(new OdbcParameter("@DataWaznosci", line.DataWaznosci));
		command.Parameters.Add(new OdbcParameter("@Ilosc", quantity));
		command.Parameters.Add(new OdbcParameter("@IdWariantu", line.IdWariantu!.Value));
		line.IdPartii = Convert.ToInt32(command.ExecuteScalar());
		command.Parameters.Clear();

		command.CommandText = """
		                      INSERT INTO magazyn.pozycje_dostaw
		                          (id_dostawy_dostawy, id_partii_partie_lekow, id_partii_surowca_partie_surowcow,
		                           ilosc, cena_zakupu)
		                      VALUES (?, ?, NULL, ?, ?);
		                      """;
		command.Parameters.Add(new OdbcParameter("@IdDostawy", deliveryId));
		command.Parameters.Add(new OdbcParameter("@IdPartii", line.IdPartii.Value));
		command.Parameters.Add(new OdbcParameter("@Ilosc", quantity));
		command.Parameters.Add(new OdbcParameter("@CenaZakupu", line.CenaZakupu));
		command.ExecuteNonQuery();
		command.Parameters.Clear();
	}

	private static void SaveRawMaterialDeliveryLine(IDbCommand command, int deliveryId, PozycjaDostawy line)
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
		command.Parameters.Add(new OdbcParameter("@NumerPartii", line.NumerPartii));
		command.Parameters.Add(new OdbcParameter("@DataWaznosci", line.DataWaznosci));
		command.Parameters.Add(new OdbcParameter("@Ilosc", line.Ilosc));
		command.Parameters.Add(new OdbcParameter("@IdSurowca", line.IdSurowca!.Value));
		line.IdPartiiSurowca = Convert.ToInt32(command.ExecuteScalar());
		command.Parameters.Clear();

		command.CommandText = """
		                      INSERT INTO magazyn.pozycje_dostaw
		                          (id_dostawy_dostawy, id_partii_partie_lekow, id_partii_surowca_partie_surowcow,
		                           ilosc, cena_zakupu)
		                      VALUES (?, NULL, ?, ?, ?);
		                      """;
		command.Parameters.Add(new OdbcParameter("@IdDostawy", deliveryId));
		command.Parameters.Add(new OdbcParameter("@IdPartiiSurowca", line.IdPartiiSurowca.Value));
		command.Parameters.Add(new OdbcParameter("@Ilosc", line.Ilosc));
		command.Parameters.Add(new OdbcParameter("@CenaZakupu", line.CenaZakupu));
		command.ExecuteNonQuery();
		command.Parameters.Clear();
	}

	private static int ToWholeQuantity(decimal quantity)
	{
		if (quantity <= 0 || quantity != decimal.Truncate(quantity))
			throw new InvalidOperationException("Ilość leku w dostawie musi być dodatnią liczbą całkowitą.");

		return Convert.ToInt32(quantity);
	}

	private static IEnumerable<PozycjaDostawy> LoadLinesForDelete(IDbCommand command, int deliveryId)
	{
		var result = new List<PozycjaDostawy>();
		command.CommandText = """
		                      SELECT id_partii_partie_lekow, id_partii_surowca_partie_surowcow, ilosc
		                      FROM magazyn.pozycje_dostaw
		                      WHERE id_dostawy_dostawy = ?;
		                      """;
		command.Parameters.Add(new OdbcParameter("@IdDostawy", deliveryId));
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
				result.Add(new PozycjaDostawy
				{
					IdPartii = reader["id_partii_partie_lekow"] == DBNull.Value
						? null
						: Convert.ToInt32(reader["id_partii_partie_lekow"]),
					IdPartiiSurowca = reader["id_partii_surowca_partie_surowcow"] == DBNull.Value
						? null
						: Convert.ToInt32(reader["id_partii_surowca_partie_surowcow"]),
					Ilosc = Convert.ToDecimal(reader["ilosc"])
				});
		}

		command.Parameters.Clear();
		return result;
	}

	private static void ReverseDrugStock(IDbCommand command, int batchId, int quantity)
	{
		command.CommandText = """
		                      UPDATE magazyn.partie_lekow
		                      SET ilosc_dostepna = ilosc_dostepna - ?
		                      WHERE id_partii = ?
		                        AND (ilosc_dostepna - ilosc_zarezerwowana) >= ?
		                      RETURNING ilosc_dostepna;
		                      """;
		command.Parameters.Add(new OdbcParameter("@Ilosc", quantity));
		command.Parameters.Add(new OdbcParameter("@IdPartii", batchId));
		command.Parameters.Add(new OdbcParameter("@IloscCheck", quantity));
		var updated = command.ExecuteScalar();
		command.Parameters.Clear();
		if (updated is null || updated == DBNull.Value)
			throw new InvalidOperationException("Nie można usunąć dostawy. Część leków została już wydana albo zarezerwowana.");
	}

	private static void ReverseRawMaterialStock(IDbCommand command, int batchId, decimal quantity)
	{
		command.CommandText = """
		                      UPDATE magazyn.partie_surowcow
		                      SET ilosc_dostepna = ilosc_dostepna - ?
		                      WHERE id_partii_surowca = ?
		                        AND (ilosc_dostepna - ilosc_zarezerwowana) >= ?
		                      RETURNING ilosc_dostepna;
		                      """;
		command.Parameters.Add(new OdbcParameter("@Ilosc", quantity));
		command.Parameters.Add(new OdbcParameter("@IdPartii", batchId));
		command.Parameters.Add(new OdbcParameter("@IloscCheck", quantity));
		var updated = command.ExecuteScalar();
		command.Parameters.Clear();
		if (updated is null || updated == DBNull.Value)
			throw new InvalidOperationException("Nie można usunąć dostawy. Część surowców została już zużyta albo zarezerwowana.");
	}
}
