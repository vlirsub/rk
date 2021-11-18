using System;
using System.Collections.Generic;
using System.Windows;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using Remote;
using Remote.Impl;
using Queue.Impl;
using Data.Level2;
using DataSource;
using DataSource.Impl;

namespace WpfApp1
{
	class Client
	{
		public CancellationTokenSource Cts { get; private set; }
		public IRemoteClient RemoteClient { get; private set; }
		public IDataSource DataSource { get; private set; }
		public Level2Window Window { get; private set; }


		public Client(CancellationTokenSource cts,
			IRemoteClient remoteClient,
			IDataSource dataSource,
			Level2Window window)
		{
			Cts = cts;
			RemoteClient = remoteClient;
			DataSource = dataSource;
			Window = window;
		}
	}

	class UIService
	{
		/// <summary>
		/// Создать окно стакана
		/// </summary>	
		/// <param name="decimals_price">Точность цены</param>
		/// <param name="decimals_quantity">Точность объема</param>
		public Client Subscribe(string symbol,
			int decimals_price,
			int decimals_quantity,
			Window Owner)
		{
			// Для каждого notify будет создан свой источник данных.
			// Возможно надо хранить источники данных и при необходимости
			// подписывать notify на уже запущенный.


			var cts = new CancellationTokenSource();

			Level2Window level2Window = new Level2Window(symbol, 
				decimals_price, 
				decimals_quantity, 
				cts);
			level2Window.Owner = Owner;

			// Очередь обновлений
			DataQueue queue = new DataQueue(cts.Token);

			// Клиент читающий данные от удалённого сервера
			IRemoteClient RemoteClient = new DataRemoteClient();
			RemoteClient.Start(symbol, queue, cts);

			// Источник данных стаканов для notify.
			// Поддерживает актуальное состояние стакана на обновлениях 
			// получаемых из очереди.
			IDataSource DataSource = new DataSourceClient();

			// Подписка на события обновления стакана + ошибки
			DataSource.Level2Changed += level2Window.Level2Changed;
			DataSource.Level2ChangedException += level2Window.Level2ChangedException;

			DataSource.Start(new Level2(decimals_price, decimals_quantity, symbol), queue, cts);

			level2Window.Show();

			return new Client(cts, RemoteClient, DataSource, level2Window);
		}
	}
}
