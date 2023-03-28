using System;
using System.Collections.Generic;

namespace Data
{
    public enum ItemKind
    {
        Bid,
        Ask
    }

    public class PriceItemEx
    {
        public double Price { get; private set; }
        public double Quantity { get; private set; }
        public ItemKind Kind { get; private set; }

        public PriceItemEx(ItemKind kind, double price, double quantity)
        {
            Kind = kind;
            Price = price;
            Quantity = quantity;
        }
    }

    namespace Level2
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


        public class DoubleRoundComparer : IComparer<double>,
            IEqualityComparer<double>
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

            public bool Equals(double x, double y)
            {
                return Math.Abs(x - y) < _epsilon;
            }
            public int GetHashCode(double obj)
            {
                return obj.GetHashCode();
            }
        }

        /// <summary>
        /// Стакан с возможностью изменения котировок
        /// </summary>
        public class Level2
        {
            public Dictionary<double, double> Bids { get; private set; }
            public Dictionary<double, double> Asks { get; private set; }
            public DateTime Time { get; private set; }

            private Int64 _lastUpdateId;
            private string _symbol;
            public string Symbol { get => _symbol; }

            private int _decimals_price;
            public int Decimals_price { get => _decimals_price; }

            private int _decimals_quantity;
            public int Decimals_quantity { get => _decimals_quantity; }

            private double _epsilon_quantity;
            public double Epsilon_quantity { get => _epsilon_quantity; }

            /// <param name="decimals_price">Точность для сравнения ключей</param>
            /// <param name="decimals_quantity">Точность для сравнения объемов</param>
            public Level2(int decimals_price, int decimals_quantity, string symbol)
            {
                DoubleRoundComparer comparer = new DoubleRoundComparer(decimals_price);

                Bids = new Dictionary<double, double>(comparer);
                Asks = new Dictionary<double, double>(comparer);

                _lastUpdateId = 0;
                _symbol = symbol;
                _decimals_price = decimals_price;
                _decimals_quantity = decimals_quantity;
                _epsilon_quantity = Math.Pow(10, -_decimals_quantity);
                Time = DateTime.MinValue;
            }

            /// <summary>
            /// Задание снимка
            /// </summary>
            /// <returns>Добавленные элементы</returns>
            public IReadOnlyCollection<PriceItemEx> SetSnapshot(Level2Snapshot l2s)
            {
                if (!string.Equals(l2s.Symbol, _symbol, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException($"Ожидается символ {_symbol}, а передано {l2s.Symbol}");

                Bids.Clear();
                Asks.Clear();
                _lastUpdateId = l2s.LastUpdateId;

                List<PriceItemEx> added = new List<PriceItemEx>();

                foreach (var b in l2s.Bids)
                    // При повторении ключей заменяем, не обновляем?
                    if (!Bids.ContainsKey(b.Price))
                        if (Math.Abs(b.Quantity) >= _epsilon_quantity)
                        {
                            Bids.Add(b.Price, b.Quantity);
                            added.Add(new PriceItemEx(ItemKind.Bid, b.Price, b.Quantity));
                        }

                foreach (var a in l2s.Asks)
                    if (!Asks.ContainsKey(a.Price))
                        if (Math.Abs(a.Quantity) >= _epsilon_quantity)
                        {
                            Asks.Add(a.Price, a.Quantity);
                            added.Add(new PriceItemEx(ItemKind.Ask, a.Price, a.Quantity));
                        }

                Time = l2s.Time;

                return added;
            }

            /// <summary>
            /// Учет изменения котировок
            /// </summary>
            /// <returns>removed, add, replace</returns>
            public (IEnumerable<double>, IEnumerable<PriceItemEx>, IEnumerable<PriceItemEx>) AddDiff(DiffData dd)
            {
                if (!string.Equals(dd.Symbol, _symbol, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException($"Ожидается символ {_symbol}, а передано {dd.Symbol}");

                if (dd.FinalID <= _lastUpdateId)
                    return (null, null, null);

                List<PriceItemEx> added = new List<PriceItemEx>();
                List<double> removed = new List<double>();
                SortedList<double, PriceItemEx> replasedNew = new SortedList<double, PriceItemEx>();

                // Bids
                foreach (var b in dd.Bids)
                    if (Bids.ContainsKey(b.Price))
                    {
                        if (Math.Abs(b.Quantity) < _epsilon_quantity)
                        {
                            if (Bids.Remove(b.Price))
                            {
                                removed.Add(b.Price);

                                var idx = replasedNew.IndexOfKey(b.Price);
                                if (idx >= 0)
                                    replasedNew.RemoveAt(idx);
                            }
                        }
                        else
                        {
                            Bids[b.Price] = b.Quantity;
                            replasedNew.Add(b.Price, new PriceItemEx(ItemKind.Bid, b.Price, b.Quantity));
                        }
                    }
                    else if (Math.Abs(b.Quantity) >= _epsilon_quantity)
                    {
                        Bids.Add(b.Price, b.Quantity);
                        added.Add(new PriceItemEx(ItemKind.Bid, b.Price, b.Quantity));
                    }

                // Asks
                foreach (var a in dd.Asks)
                    if (Asks.ContainsKey(a.Price))
                    {
                        if (Math.Abs(a.Quantity) < _epsilon_quantity)
                        {

                            if (Asks.Remove(a.Price))
                            {
                                removed.Add(a.Price);
                                var idx = replasedNew.IndexOfKey(a.Price);
                                if (idx >= 0)
                                    replasedNew.RemoveAt(idx);
                            }
                        }
                        else
                        {
                            Asks[a.Price] = a.Quantity;
                            replasedNew.Add(a.Price, new PriceItemEx(ItemKind.Ask, a.Price, a.Quantity));
                        }
                    }
                    else if (Math.Abs(a.Quantity) >= _epsilon_quantity)
                    {
                        Asks.Add(a.Price, a.Quantity);
                        added.Add(new PriceItemEx(ItemKind.Ask, a.Price, a.Quantity));
                    }

                Time = dd.Time;
                _lastUpdateId = dd.FinalID + 1;

                return (removed, added, replasedNew.Values);
            }
        }
    }
}
