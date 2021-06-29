using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using System.Globalization;
using System.Threading;

using Data;
using Data.Level2;
using Remote.Data;

namespace Remote
{
	/// <summary>
	/// Добавление данных в очередь
	/// </summary>
	public interface IDataAdd
	{
		void Enqueue(Level2Snapshot l2s);
		void Enqueue(DiffData dd);
		void Enqueue(Exception E);
	}

	public interface IRemoteClient
	{
		Task Start(string symbol, IDataAdd queue, CancellationTokenSource cts);
	}

	namespace Impl
	{
		/// <summary>
		/// Получение снимка стакана
		/// </summary>
		class DataRemoteSnapshotClient : IDisposable
		{
			private readonly string _symbol;
			private Uri _uri;
			public DataRemoteSnapshotClient(string symbol, Uri uri)
			{
				_symbol = symbol;
				_uri = uri ?? throw new ArgumentNullException("uri");
			}

			public Level2Snapshot GetSnapshot()
			{
				using (WebClient wc = new WebClient())
				{
					string StringSnapshot = wc.DownloadString(_uri);

					Level2SnapshotRemote l2sr = JsonSerializer.Deserialize<Level2SnapshotRemote>(StringSnapshot);

					var Bids = l2sr.Bids.Select(
						s => new PriceItem(
							Double.Parse(s[0], CultureInfo.InvariantCulture),
							Double.Parse(s[1], CultureInfo.InvariantCulture))
						);
					var Asks = l2sr.Asks.Select(
						s => new PriceItem(
							Double.Parse(s[0], CultureInfo.InvariantCulture),
							Double.Parse(s[1], CultureInfo.InvariantCulture))
						);

					Level2Snapshot l2s = new Level2Snapshot(
						_symbol,
						DateTime.UtcNow,
						l2sr.LastUpdateId,
						Bids.ToArray(),
						Asks.ToArray()
						);

					return l2s;
				}
			}

			public void Dispose()
			{
				// пока пустой
			}
		}

		/// <summary>
		/// Получение изменения котировок
		/// </summary>
		class DataRemoteDiffClient : IDisposable
		{
			private readonly CancellationToken _ct;
			ClientWebSocket _socket;
			byte[] _bytes = new byte[4096];
			private Uri _uri;

			public DataRemoteDiffClient(Uri uri, CancellationToken ct, int bufsize = 4096)
			{
				_ct = ct;
				_socket = new ClientWebSocket();
				_bytes = new byte[bufsize];
				_uri = uri ?? throw new ArgumentNullException("uri");
			}

			public void Dispose()
			{
				_socket.Dispose();
			}

			public void Connect()
			{
				_socket.ConnectAsync(_uri, _ct).Wait();
			}

			public WebSocketState State { get { return _socket.State; } }

			public DiffData GetDiffData()
			{
				var Buffer = new ArraySegment<byte>(_bytes);

				if (_socket.State != WebSocketState.Open)
					throw new Exception("Socket state not open");


				string Msg = "";
				try
				{
					WebSocketReceiveResult Result = _socket.ReceiveAsync(Buffer, _ct).Result;
					byte[] msgBytes = Buffer.Skip(Buffer.Offset).Take(Result.Count).ToArray();
					Msg = Encoding.UTF8.GetString(msgBytes);

					DiffDataRemote ev = JsonSerializer.Deserialize<DiffDataRemote>(Msg);

					var Bids = ev.Bids.Select(
						s => new PriceItem(
							Double.Parse(s[0], CultureInfo.InvariantCulture),
							Double.Parse(s[1], CultureInfo.InvariantCulture))
						);
					var Asks = ev.Asks.Select(
						s => new PriceItem(
							Double.Parse(s[0], CultureInfo.InvariantCulture),
							Double.Parse(s[1], CultureInfo.InvariantCulture))
						);

					DateTime DT = new DateTime(1970, 1, 1) + TimeSpan.FromMilliseconds(ev.Time);

					DiffData dd = new DiffData(
						DT,
						ev.Symbol,
						ev.FirstUpdateID,
						ev.FinalUpdateID,
						Bids.ToArray(),
						Asks.ToArray()
						);

					return dd;
				}
				catch (Exception E)
				{
					throw new Exception(Msg, E);
				}
			}
		}

		/// <summary>
		/// Получение удаленных данных
		/// </summary>
		public class DataRemoteClient : IRemoteClient
		{
			public DataRemoteClient()
			{
			}

			public Task Start(string symbol, IDataAdd queue, CancellationTokenSource cts)
			{

				return Task.Factory.StartNew(
					() =>
					{
						try
						{
							// Адреса для запроса данных. 
							// Выносятся в настройки.

							Uri SnapshotUri = new Uri($"https://api.binance.com/api/v3/depth?symbol={symbol.ToUpper()}&limit=1000");
							Uri WSSUri = new Uri($"wss://stream.binance.com:9443/ws/{symbol}@depth@1000ms".ToLower());

							using (DataRemoteSnapshotClient RemoteClient = new DataRemoteSnapshotClient(symbol, SnapshotUri))
								while (true)
								{
									while (true)
									{
										// Получение снимка стакана.
										// Если сервер не будет доступен - будут попытки пподключения

										try
										{
											Level2Snapshot l2s = RemoteClient.GetSnapshot();
											queue.Enqueue(l2s);
											break;
										}
										catch (WebException E)
										{
											queue.Enqueue(E);
											// В зависимости о типа ошибки можно принять разные действия
											Thread.Sleep(1000);
										}
									}

									using (DataRemoteDiffClient RemoteDiffClient = new DataRemoteDiffClient(WSSUri, cts.Token))
									{
										RemoteDiffClient.Connect();

										while (RemoteDiffClient.State == WebSocketState.Open)
										{
											DiffData dd = RemoteDiffClient.GetDiffData();

											queue.Enqueue(dd);
										}
									}
								}
						}
						catch (Exception E)
						{
							queue.Enqueue(E);
						}

					}, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
			}
		}
	}
}
