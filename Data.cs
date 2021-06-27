using System;
using System.Collections.Generic;

namespace Data
{
	public struct PriceItem
	{
		public double Price;
		public double Quantity;

		public PriceItem(double price, double quantity)
		{
			Price = price;
			Quantity = quantity;
		}
	}

	namespace Level2
	{
		/// <summary>
		/// Изменение котировок
		/// </summary>
		public class DiffData
		{
			public DateTime Time { get; private set; }
			public string Symbol { get; private set; }
			public Int64 FirstID { get; private set; }
			public Int64 FinalID { get; private set; }
			public PriceItem[] Bids { get; private set; }
			public PriceItem[] Asks { get; private set; }

			public DiffData(DateTime time, string symbol, Int64 firstID, Int64 finalID,
				PriceItem[] bids, PriceItem[] asks)
			{
				Time = time;
				Symbol = symbol;
				FirstID = firstID;
				FinalID = finalID;
				Bids = bids;
				Asks = asks;
			}
		}


		/// <summary>
		/// Снимок стакана
		/// </summary>
		public class Level2Snapshot
		{
			public string Symbol { get; private set; }
			public Int64 LastUpdateId { get; private set; }
			public PriceItem[] Bids { get; private set; }
			public PriceItem[] Asks { get; private set; }
			public DateTime Time { get; private set; }

			public Level2Snapshot(
				string symbol,
				DateTime time,
				Int64 lastUpdateId,
				PriceItem[] bids,
				PriceItem[] asks)
			{
				Symbol = symbol;
				Time = time;
				LastUpdateId = lastUpdateId;
				Bids = bids;
				Asks = asks;
			}
		}


		public class DoubleRoundComparer : IComparer<double>
		{
			private readonly double _epsilon;

			public DoubleRoundComparer(int decimals)
			{
				_epsilon = Math.Pow(10, -decimals);
			}
			public int Compare(double x, double y)
			{
				if (Math.Abs(x - y) < _epsilon)
					return 0;
				else if (x < y)
					return 1;
				else
					return -1;
			}			
		}

		/// <summary>
		/// Стакан с возможностью изменения котировок
		/// </summary>
		public class Level2
		{
			public SortedDictionary<double, double> Bids { get; private set; }
			public SortedDictionary<double, double> Asks { get; private set; }
			public DateTime Time { get; private set; }

			private Int64 _lastUpdateId;
			private string _symbol;
			private int _decimals_quantity;
			private double _epsilon_quantity;

			/// <param name="decimals_key">Точность для сравнения ключей</param>
			/// <param name="decimals_quantity">Точность для сравнения объемов</param>
			public Level2(int decimals_key, int decimals_quantity, string symbol)
			{
				DoubleRoundComparer comparer = new DoubleRoundComparer(decimals_key);

				Bids = new SortedDictionary<double, double>(comparer);
				Asks = new SortedDictionary<double, double>(comparer);

				_lastUpdateId = 0;
				_symbol = symbol;
				_decimals_quantity = decimals_quantity;
				_epsilon_quantity = Math.Pow(10, -_decimals_quantity);
				Time = DateTime.MinValue;
			}

			/// <summary>
			/// Задание снимка
			/// </summary>
			public void SetSnapshot(Level2Snapshot l2s)
			{
				if (!string.Equals(l2s.Symbol, _symbol, StringComparison.OrdinalIgnoreCase))
					throw new ArgumentException($"Ожидается символ {_symbol}, а передано {l2s.Symbol}");

				Bids.Clear();
				Asks.Clear();
				_lastUpdateId = l2s.LastUpdateId;

				foreach (var b in l2s.Bids)
					// При повторении ключей заменяем, не обновляем?
					if (!Bids.ContainsKey(b.Price))
						if (Math.Abs(b.Quantity) >= _epsilon_quantity)
							Bids.Add(b.Price, b.Quantity);

				foreach (var a in l2s.Asks)
					if (!Asks.ContainsKey(a.Price))
						if (Math.Abs(a.Quantity) >= _epsilon_quantity)
							Asks.Add(a.Price, a.Quantity);

				Time = l2s.Time;
			}

			/// <summary>
			/// Учет изменения котировок
			/// </summary>
			public void AddDiff(DiffData dd)
			{
				if (!string.Equals(dd.Symbol, _symbol, StringComparison.OrdinalIgnoreCase))
					throw new ArgumentException($"Ожидается символ {_symbol}, а передано {dd.Symbol}");

				if (dd.FinalID <= _lastUpdateId)
					return;

				// Bids
				foreach (var b in dd.Bids)
					if (Bids.ContainsKey(b.Price))
						if (Math.Abs(b.Quantity) < _epsilon_quantity)
							Bids.Remove(b.Price);
						else
							Bids[b.Price] = b.Quantity;
					else
						if (Math.Abs(b.Quantity) >= _epsilon_quantity)
							Bids.Add(b.Price, b.Quantity);

				// Asks
				foreach (var a in dd.Asks)
					if (Asks.ContainsKey(a.Price))
						if (Math.Abs(a.Quantity) < _epsilon_quantity)
							Asks.Remove(a.Price);
						else
							Asks[a.Price] = a.Quantity;
					else
						if (Math.Abs(a.Quantity) >= _epsilon_quantity)
							Asks.Add(a.Price, a.Quantity);

				Time = dd.Time;
				_lastUpdateId = dd.FinalID + 1;
			}
		}
	}
}
