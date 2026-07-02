using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using Apteka.Models;

namespace Apteka.Repositories;

public class OrderRepository(DatabaseService dbService, AddressRepository addressRepository)
{
	public IEnumerable<Dostawca> GetSuppliers()
	{
		var suppliers = new List<Dostawca>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT id_dostawcy, nazwa, "NIP", id_adresu_adresy
		                      FROM magazyn.dostawcy
		                      ORDER BY nazwa;
		                      """;

		using var reader = command.ExecuteReader();
		while (reader.Read())
		{
			var supplier = new Dostawca
			{
				Id = Convert.ToInt32(reader["id_dostawcy"]),
				Nazwa = reader["nazwa"].ToString() ?? string.Empty,
				NIP = reader["NIP"].ToString() ?? string.Empty,
				IdAdresu = Convert.ToInt32(reader["id_adresu_adresy"])
			};
			supplier.Adres = addressRepository.GetAdresById(supplier.IdAdresu);
			suppliers.Add(supplier);
		}

		return suppliers;
	}

	public IEnumerable<Zamowienie> GetAll()
	{
		var orders = new List<Zamowienie>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT z.id_zamowienia, z.data_utworzenia, z.status,  z.typ,
		                             d.id_dostawcy, d.nazwa, d."NIP", d.id_adresu_adresy
		                      FROM magazyn.zamowienia z
		                      JOIN magazyn.dostawcy d on z.id_dostawcy_dostawcy = d.id_dostawcy
		                      ORDER BY data_utworzenia DESC;
		                      """;

		using var reader = command.ExecuteReader();
		while (reader.Read())
			orders.Add(new Zamowienie
			{
				Id = Convert.ToInt32(reader["id_zamowienia"]),
				DataUtworzenia = Convert.ToDateTime(reader["data_utworzenia"]),
				Status = reader["status"].ToString() ?? string.Empty,
				Typ = reader["typ"].ToString() ?? string.Empty,
				IdDostawcy = Convert.ToInt32(reader["id_dostawcy"]),
				Dostawca = new Dostawca
				{
					Id = Convert.ToInt32(reader["id_dostawcy"]),
					Nazwa = reader["nazwa"].ToString() ?? string.Empty,
					NIP = reader["NIP"].ToString() ?? string.Empty,
					IdAdresu = Convert.ToInt32(reader["id_adresu_adresy"])
				}
			});

		foreach (var order in orders)
		{
			var adres = addressRepository.GetAdresById(order.Dostawca!.IdAdresu);
			order.Dostawca!.Adres = adres;
		}

		return orders;
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

	public IEnumerable<PozycjaZamowienia> GetReorderSuggestions(decimal minimumQuantity, decimal targetQuantity)
	{
		if (minimumQuantity < 0) throw new InvalidOperationException("Minimalny stan nie może być ujemny.");
		if (targetQuantity <= minimumQuantity)
			throw new InvalidOperationException("Poziom docelowy musi być większy od minimalnego.");

		var result = new List<PozycjaZamowienia>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT w.id_wariantu,
		                             l.nazwa || ' ' || w.dawkowanie || ' x' || w.ilosc AS nazwa,
		                             COALESCE(SUM(
		                                 CASE
		                                     WHEN pl.data_waznosci >= CURRENT_DATE
		                                     THEN pl.ilosc_dostepna - pl.ilosc_zarezerwowana
		                                     ELSE 0
		                                 END
		                             ), 0)::numeric AS ilosc_dostepna
		                      FROM apteka.warianty_lekow w
		                      JOIN apteka.leki l ON l.id_leku = w.id_leku_leki
		                      LEFT JOIN magazyn.partie_lekow pl ON pl.id_wariantu_warianty_lekow = w.id_wariantu
		                      GROUP BY w.id_wariantu, l.nazwa, w.dawkowanie, w.ilosc
		                      HAVING COALESCE(SUM(
		                          CASE
		                              WHEN pl.data_waznosci >= CURRENT_DATE
		                              THEN pl.ilosc_dostepna - pl.ilosc_zarezerwowana
		                              ELSE 0
		                          END
		                      ), 0) <= ?
		                      ORDER BY nazwa;
		                      """;
		command.Parameters.Add(new OdbcParameter("@MinimumQuantity", minimumQuantity));
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
			{
				var available = Convert.ToDecimal(reader["ilosc_dostepna"]);
				result.Add(new PozycjaZamowienia
				{
					IdWariantu = Convert.ToInt32(reader["id_wariantu"]),
					TypProduktu = "Lek",
					Nazwa = reader["nazwa"].ToString() ?? string.Empty,
					Ilosc = targetQuantity - available,
					CenaSzacowana = 0
				});
			}
		}

		command.Parameters.Clear();
		command.CommandText = """
		                      SELECT s.id_surowca, s.nazwa_surowca || ' (' || s.jednostka || ')' AS nazwa,
		                             COALESCE(SUM(
		                                 CASE
		                                     WHEN ps.data_waznosci >= CURRENT_DATE
		                                     THEN ps.ilosc_dostepna - ps.ilosc_zarezerwowana
		                                     ELSE 0
		                                 END
		                             ), 0)::numeric AS ilosc_dostepna
		                      FROM magazyn.surowce s
		                      LEFT JOIN magazyn.partie_surowcow ps ON ps.id_surowca_surowce = s.id_surowca
		                      GROUP BY s.id_surowca, s.nazwa_surowca, s.jednostka
		                      HAVING COALESCE(SUM(
		                          CASE
		                              WHEN ps.data_waznosci >= CURRENT_DATE
		                              THEN ps.ilosc_dostepna - ps.ilosc_zarezerwowana
		                              ELSE 0
		                          END
		                      ), 0) <= ?
		                      ORDER BY s.nazwa_surowca;
		                      """;
		command.Parameters.Add(new OdbcParameter("@MinimumQuantity", minimumQuantity));
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
			{
				var available = Convert.ToDecimal(reader["ilosc_dostepna"]);
				result.Add(new PozycjaZamowienia
				{
					IdSurowca = Convert.ToInt32(reader["id_surowca"]),
					TypProduktu = "Surowiec",
					Nazwa = reader["nazwa"].ToString() ?? string.Empty,
					Ilosc = targetQuantity - available,
					CenaSzacowana = 0
				});
			}
		}

		return result.Where(x => x.Ilosc > 0);
	}

	public IEnumerable<PozycjaZamowienia> GetLines(int orderId)
	{
		if (orderId <= 0) return [];
		var lines = new List<PozycjaZamowienia>();
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      SELECT p.id_pozycji_zamowienia,
		                             p.id_zamowienia_zamowienia,
		                             p.id_wariantu_warianty_lekow,
		                             p.id_surowca_surowce,
		                             CASE
		                                 WHEN p.id_wariantu_warianty_lekow IS NOT NULL
		                                 THEN l.nazwa || ' ' || w.dawkowanie || ' x' || w.ilosc
		                                 ELSE s.nazwa_surowca || ' (' || s.jednostka || ')'
		                             END AS nazwa,
		                             p.ilosc,
		                             p.cena_szacowana,
		                             CASE
		                                 WHEN p.id_wariantu_warianty_lekow IS NOT NULL THEN 'Lek'
		                                 ELSE 'Surowiec'
		                             END AS typ_produktu
		                      FROM magazyn.pozycje_zamowien p
		                      LEFT JOIN apteka.warianty_lekow w ON w.id_wariantu = p.id_wariantu_warianty_lekow
		                      LEFT JOIN apteka.leki l ON l.id_leku = w.id_leku_leki
		                      LEFT JOIN magazyn.surowce s ON s.id_surowca = p.id_surowca_surowce
		                      WHERE p.id_zamowienia_zamowienia = ?
		                      ORDER BY p.id_pozycji_zamowienia;
		                      """;
		command.Parameters.Add(new OdbcParameter("@IdZamowienia", orderId));

		using var reader = command.ExecuteReader();
		while (reader.Read())
			lines.Add(new PozycjaZamowienia
			{
				Id = Convert.ToInt32(reader["id_pozycji_zamowienia"]),
				IdZamowienia = Convert.ToInt32(reader["id_zamowienia_zamowienia"]),
				IdWariantu = reader["id_wariantu_warianty_lekow"] == DBNull.Value
					? null
					: Convert.ToInt32(reader["id_wariantu_warianty_lekow"]),
				IdSurowca = reader["id_surowca_surowce"] == DBNull.Value
					? null
					: Convert.ToInt32(reader["id_surowca_surowce"]),
				TypProduktu = reader["typ_produktu"].ToString() ?? string.Empty,
				Nazwa = reader["nazwa"].ToString() ?? string.Empty,
				Ilosc = Convert.ToDecimal(reader["ilosc"]),
				CenaSzacowana = Convert.ToDecimal(reader["cena_szacowana"])
			});

		return lines;
	}

	public void Add(Zamowienie zamowienie)
	{
		Add(zamowienie, []);
	}

	public void Add(Zamowienie zamowienie, IEnumerable<PozycjaZamowienia> pozycje)
	{
		using var connection = dbService.CreateConnection();
		using var transaction = connection.BeginTransaction();
		try
		{
			using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = """
			                      INSERT INTO magazyn.zamowienia (data_utworzenia, status, typ, id_dostawcy_dostawcy) 
			                      VALUES (?, ?, ?, ?) 
			                      RETURNING id_zamowienia;
			                      """;
			command.Parameters.Add(new OdbcParameter("@Data", zamowienie.DataUtworzenia));
			command.Parameters.Add(new OdbcParameter("@Status", zamowienie.Status));
			command.Parameters.Add(new OdbcParameter("@Typ", zamowienie.Typ));
			command.Parameters.Add(new OdbcParameter("@IdDostawcy", zamowienie.IdDostawcy));
			zamowienie.Id = Convert.ToInt32(command.ExecuteScalar());
			command.Parameters.Clear();

				foreach (var pozycja in pozycje)
				{
					command.CommandText = """
					                      INSERT INTO magazyn.pozycje_zamowien
					                          (id_zamowienia_zamowienia, id_wariantu_warianty_lekow,
					                           id_surowca_surowce, ilosc, cena_szacowana)
					                      VALUES (?, ?, ?, ?, ?);
					                      """;
					command.Parameters.Add(new OdbcParameter("@IdZamowienia", zamowienie.Id));
					command.Parameters.Add(new OdbcParameter("@IdWariantu", pozycja.IdWariantu.HasValue
						? (object)pozycja.IdWariantu.Value
						: DBNull.Value));
					command.Parameters.Add(new OdbcParameter("@IdSurowca", pozycja.IdSurowca.HasValue
						? (object)pozycja.IdSurowca.Value
						: DBNull.Value));
					command.Parameters.Add(new OdbcParameter("@Ilosc", pozycja.Ilosc));
					command.Parameters.Add(new OdbcParameter("@CenaSzacowana", pozycja.CenaSzacowana));
					command.ExecuteNonQuery();
				command.Parameters.Clear();
			}

			transaction.Commit();
		}
		catch
		{
			transaction.Rollback();
			throw;
		}
	}

	public void UpdateStatus(int orderId, string status)
	{
		if (orderId <= 0) return;
		if (!IsAllowedStatus(status))
			throw new InvalidOperationException("Nieprawidłowy status zamówienia.");

		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = """
		                      UPDATE magazyn.zamowienia
		                      SET status = ?
		                      WHERE id_zamowienia = ?;
		                      """;
		command.Parameters.Add(new OdbcParameter("@Status", status));
		command.Parameters.Add(new OdbcParameter("@IdZamowienia", orderId));
		command.ExecuteNonQuery();
	}

	public void DeleteLine(int lineId)
	{
		if (lineId <= 0) return;
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "DELETE FROM magazyn.pozycje_zamowien WHERE id_pozycji_zamowienia = ?;";
		command.Parameters.Add(new OdbcParameter("@IdPozycji", lineId));
		command.ExecuteNonQuery();
	}

	public void Delete(int orderId)
	{
		if (orderId <= 0) return;
		using var connection = dbService.CreateConnection();
		using var command = connection.CreateCommand();
		command.CommandText = "DELETE FROM magazyn.zamowienia WHERE id_zamowienia = ?;";
		command.Parameters.Add(new OdbcParameter("@IdZamowienia", orderId));
		command.ExecuteNonQuery();
	}

	private static bool IsAllowedStatus(string status)
	{
		return status is "Nowe" or "Zatwierdzone" or "Zrealizowane" or "Anulowane" or "Archiwalne";
	}
}
