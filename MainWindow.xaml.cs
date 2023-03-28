using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace WpfApp1
{
	/// <summary>
	/// Основное окно приложения
	/// </summary>
	public partial class MainWindow : Window
	{
		/// <summary>
		/// Сервис для запуска чтения стаканов
		/// </summary>
		private UIService _service = new UIService();

		public MainWindow()
		{
			InitializeComponent();

			WindowStartupLocation = WindowStartupLocation.CenterScreen;
		}

		private void StartLevel2(string symbol,
			int decimals_price,
			int decimals_quantity)
		{
			try
			{
				_service.Subscribe(symbol, decimals_price, decimals_quantity, this);
			}
			catch (Exception E)
			{
				MessageBox.Show(E.ToString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void Btn_BNBBTC_Click(object sender, RoutedEventArgs e)
		{
			// Откуда то получать. Здесь магические числа для примера. Взяты с сайта.
			const string symbol = "bnbbtc";
			// Точность цены
			int decimals_price = 6;
			// Точность объема
			int decimals_quantity = 2;

			StartLevel2(symbol, decimals_price, decimals_quantity);
		}

		private void Btn_BTCRUB_Click(object sender, RoutedEventArgs e)
		{
			// Откуда то получать. Здесь магические числа для примера. Взяты с сайта.
			const string symbol = "btcrub";
			// Точность цены
			int decimals_price = 0;
			// Точность объема
			int decimals_quantity = 6;

			StartLevel2(symbol, decimals_price, decimals_quantity);
		}
	}
}
