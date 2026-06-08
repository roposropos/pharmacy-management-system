using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using Apteka.ViewModels.Grid;

namespace Apteka.Repositories;

public class SaleRepository(DatabaseService dbService)
{
	private readonly Dictionary<int, decimal> _cenyLekow = new();
	private readonly Dictionary<int, int> _iloscLekow = new();
	private IDbConnection? _connection;
	private int _orderId;
	private IDbTransaction? _transaction;

	public void BeginTransaction()
	{
		if (_connection != null) throw new InvalidOperationException();
		_connection = dbService.CreateConnection();
		_transaction = _connection.BeginTransaction();
		_orderId = -1;
	}

	public void Faktura()
	{
		if (_connection == null) throw new InvalidOperationException();

		using var command = _connection.CreateCommand();
		command.Transaction = _transaction;

		command.CommandText = """
		                      INSERT INTO apteka.sprzedaze (typ_dokumentu, data_sprzedazy)
		                      VALUES ('Faktura', NOW())
		                      RETURNING id_sprzedazy;
		                      """;
		_orderId = Convert.ToInt32(command.ExecuteScalar());
	}

	public void Paragon()
	{
		if (_connection == null) throw new InvalidOperationException();

		using var command = _connection.CreateCommand();
		command.Transaction = _transaction;

		command.CommandText = """
		                      INSERT INTO apteka.sprzedaze (typ_dokumentu, data_sprzedazy)
		                      VALUES ('Faktura', NOW())
		                      RETURNING id_sprzedazy;
		                      """;
		_orderId = Convert.ToInt32(command.ExecuteScalar());
	}

	public void Add(PozycjaSprzedazyViewModel item)
	{
		var partiaId = item.Partia.Id;
		_iloscLekow.TryAdd(partiaId, 0);
		_cenyLekow.TryAdd(partiaId, item.Price);
		_iloscLekow[partiaId]++;
	}

	public int Finish()
	{
		if (_transaction == null || _connection == null) _orderId = -1;
		if (_orderId == -1) throw new InvalidOperationException();
		using var command = _connection!.CreateCommand();
		command.Transaction = _transaction;
		foreach (var (partiaId, ilosc) in _iloscLekow)
		{
			var cena = _cenyLekow[partiaId];
			command.CommandText = """
			                      INSERT INTO apteka.pozycja_sprzedazy (ilosc, cena_jednostkowa, typ_produktu, id_sprzedazy_sprzedaze)
			                      VALUES (?, ?, 'Lek', ?)
			                      RETURNING id_pozycji;
			                      """;
			command.Parameters.Add(new OdbcParameter("@Ilosc", ilosc));
			command.Parameters.Add(new OdbcParameter("@Cena", cena));
			command.Parameters.Add(new OdbcParameter("@OrderId", _orderId));
			var pozycja = Convert.ToInt32(command.ExecuteScalar());
			command.Parameters.Clear();
			command.CommandText =
				"INSERT INTO apteka.lek_w_pozycji_sprzedazy (id_partii_partie_lekow, id_pozycji_pozycja_sprzedazy) VALUES (?, ?);";
			command.Parameters.Add(new OdbcParameter("@PartiaId", partiaId));
			command.Parameters.Add(new OdbcParameter("@PozycjaId", pozycja));
			try
			{
				command.ExecuteNonQuery();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			command.Parameters.Clear();
		}

		var total = _cenyLekow.Sum(x => x.Value * _iloscLekow[x.Key]);
		command.CommandText = "UPDATE apteka.sprzedaze SET kwota = ? WHERE id_sprzedazy = ?;";
		command.Parameters.Add(new OdbcParameter("@Kwota", total));
		command.Parameters.Add(new OdbcParameter("@OrderId", _orderId));
		command.ExecuteNonQuery();

		_transaction!.Commit();
		_connection!.Close();
		_connection = null;
		_transaction = null;
		_iloscLekow.Clear();
		_cenyLekow.Clear();
		return _orderId;
	}
}