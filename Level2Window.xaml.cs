using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Threading;

using Data.Level2;
using DataSource;

namespace WpfApp1
{
    using Data;

    class ItemView
    {
        public double Price { get; private set; }
        public double Quantity { get; set; }
        public Brush Background { get; private set; }

        public ItemKind Kind { get; private set; }

        public ItemView(double price, double quantity, Brush background, ItemKind kind)
        {
            Price = price;
            Quantity = quantity;
            Background = background;
            Kind = kind;
        }
    }

    /// <summary>
    /// Формирование форматной строки
    /// </summary>
    class Formater
    {
        static public string StringFormat(int decimals)
        {
            if (decimals > 0)
                return "0." + new string('0', decimals);
            else
                return "0";
        }
    }


    public class L2ViewModel : INotifyCollectionChanged, IEnumerable
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add { _items.CollectionChanged += value; }
            remove { _items.CollectionChanged -= value; }
        }

        private readonly Level2 _l2;

        private SortedList<double, int> _index = new SortedList<double, int>();
        ObservableCollection<ItemView> _items = new ObservableCollection<ItemView>();

        public string Symbol { get => _l2.Symbol; }
        public int Decimals_price { get => _l2.Decimals_price; }
        public int Decimals_quantity { get => _l2.Decimals_quantity; }

        public double Epsilon_quantity { get => _l2.Epsilon_quantity; }

        public L2ViewModel(Level2 l2)
        {
            _l2 = l2 ?? throw new ArgumentNullException(nameof(l2));
        }

        public void ChangeLevel2Remove(IEnumerable<double> remove)
        {
            if (!remove?.Any() ?? true)
                return;

            foreach (var item in remove)
            {
                var idx = _index.IndexOfKey(item);
                _items.RemoveAt(idx);
                _index.RemoveAt(idx);
            }
        }

        public void ChangeLevel2Replace(IEnumerable<PriceItemEx> replace)
        {
            if (!replace?.Any() ?? true)
                return;

            foreach (var item in replace)
            {
                var idx = _index.IndexOfKey(item.Price);
                var newItem = new ItemView(item.Price, item.Quantity,
                    item.Kind == ItemKind.Ask ? Brushes.Red : Brushes.Green, item.Kind);
                _items[idx] = newItem;
            }
        }

        public void ChangeLevel2Add(IEnumerable<PriceItemEx> add)
        {
            if (!add?.Any() ?? true)
                return;

            foreach (var item in add)
            {
                var newItem = new ItemView(item.Price, item.Quantity,
                    item.Kind == ItemKind.Ask ? Brushes.Red : Brushes.Green, item.Kind);
                
                _index.Add(item.Price, 0);
                var idx = _index.IndexOfKey(item.Price);

                _items.Insert(idx, newItem);

                if (_items.Count != _index.Count)
                    throw new Exception($"Различается количество элементов {_items.Count} != {_index.Count}");
            }
        }

        public void ChangeLevel2Diff(
            IEnumerable<double> remove,
            IEnumerable<PriceItemEx> add,
            IEnumerable<PriceItemEx> replace)
        {
            ChangeLevel2Remove(remove);

            ChangeLevel2Replace(replace);

            ChangeLevel2Add(add);
        }

        public IEnumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }

    /// <summary>
    /// Отображение стакана
    /// </summary>
    public partial class Level2Window : Window, ILevel2Notify
    {
        private CancellationTokenSource _cts;
        private L2ViewModel _l2;

        public Level2Window(L2ViewModel l2,
            CancellationTokenSource cts)
        {
            InitializeComponent();

            _l2 = l2 ?? throw new ArgumentNullException(nameof(l2));
            _cts = cts;
            Title = _l2.Symbol.ToUpper();

            Clm_Price.Binding.StringFormat = string.Format("{{0:{0}}}",
                Formater.StringFormat(_l2.Decimals_price));
            Clm_Quantity.Binding.StringFormat = string.Format("{{0:{0}}}",
                Formater.StringFormat(_l2.Decimals_quantity));

            Grid_Level2.ItemsSource = _l2;
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
        }

        /// <summary>
        /// Изменение стакана
        /// </summary>
        public void OnLevel2DiffChanged(
            IEnumerable<double> remove,
            IEnumerable<PriceItemEx> add,
            IEnumerable<PriceItemEx> replace)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
                this.ChangeLevel2Diff(remove, add, replace))
            );
        }

        /// <summary>
        /// Ошибка при чтении данных
        /// </summary>
        public void OnLevel2ChangedException(Exception E)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
                this.Level2Exception(E))
                );
        }

        /// <summary>
        /// Изменение стакана
        /// </summary>
        private void ChangeLevel2Diff(
            IEnumerable<double> remove,
            IEnumerable<PriceItemEx> add,
            IEnumerable<PriceItemEx> replace)
        {
            _l2.ChangeLevel2Diff(remove, add, replace);
        }

        private void Level2Exception(Exception E)
        {
            MessageBox.Show(E.ToString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
