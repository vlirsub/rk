using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Threading;

using Data;
using DataSource;

namespace WpfApp1
{
	class ItemView
	{
		public double Price { get; private set; }
		public double Quantity { get; private set; }
		public Brush Background { get; private set; }

		public ItemView(double price, double quantity, Brush background)
		{
			Price = price;
			Quantity = quantity;
			Background = background;
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
	/// <summary>
	/// Отображение стакана
	/// </summary>
	public partial class Level2Window : Window, ILevel2Notify
	{
		private ObservableCollection<ItemView> _items = new ObservableCollection<ItemView>();
		private CancellationTokenSource _cts;

		public Level2Window(string symbol,
			int decimal_price,
			int decimals_quantity,
			CancellationTokenSource cts)
		{
			InitializeComponent();

			_cts = cts;
			Title = symbol.ToUpper();

			Grid_Level2.ItemsSource = _items;
			
			
			Clm_Price.Binding.StringFormat = string.Format("{{0:{0}}}", 
				Formater.StringFormat(decimal_price));
			Clm_Quantity.Binding.StringFormat = string.Format("{{0:{0}}}",
				Formater.StringFormat(decimals_quantity));
		}

		private void Window_Unloaded(object sender, RoutedEventArgs e)
		{
			_cts.Cancel();
		}

		/// <summary>
		/// Обновление стакана. Вызывается в другом потоке.
		/// </summary>		
		public void Level2Changed(PriceItem[] bids, PriceItem[] asks)
		{
			this.Dispatcher.BeginInvoke(new Action(() => 
				this.ChangeLevel2(bids, asks))
				);
		}

		/// <summary>
		/// Ошибка при чтении данных
		/// </summary>
		public void Level2ChangedException(Exception E)
		{
			this.Dispatcher.BeginInvoke(new Action(() =>
				this.Level2Exception(E))
				);
		}

		private void ChangeLevel2(PriceItem[] bids, PriceItem[] asks)
		{
			_items.Clear();
			foreach (var p in asks)
				_items.Add(new ItemView(p.Price, p.Quantity, Brushes.Red));

			ItemView item = null;
			if (_items.Count > 0)
				item = _items[_items.Count - 1];
			
			foreach (var p in bids)
				_items.Add(new ItemView(p.Price, p.Quantity, Brushes.Green));

			Grid_Level2.ScrollIntoView(item);
		}

		private void Level2Exception(Exception E)
		{
			MessageBox.Show(E.ToString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
}
