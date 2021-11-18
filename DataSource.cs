using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace DataSource
{
	using Data;
	using Queue;
	using Data.Level2;

	public delegate void Level2ChangedHandler(PriceItem[] bids, PriceItem[] asks);
	public delegate void Level2ChangedExceptionHandler(Exception E);

	public interface ILevel2Notify
	{
		void Level2Changed(PriceItem[] bids, PriceItem[] asks);
		void Level2ChangedException(Exception E);
	}

	public interface IDataSource
	{
		event Level2ChangedHandler Level2Changed;
		event Level2ChangedExceptionHandler Level2ChangedException;
		Task Start(Level2 level2, IDataGet queue, CancellationTokenSource cts);
	}

	namespace Impl
	{
		public class DataSourceClient : IDataSource
		{
			public event Level2ChangedHandler Level2Changed;
			public event Level2ChangedExceptionHandler Level2ChangedException;

			public Task Start(Level2 level2, IDataGet queue, CancellationTokenSource cts)
			{
				return Task.Factory.StartNew(
				() =>
				{
					while (true)
					{
						try
						{
							if (queue.Dequeue(out DataPacket[] packets, cts.Token))
								for (var i = 0; i < packets.Length; i++)
								{
									if (packets[i].Kind == DataPacketKind.Error)
									{
										Exception E = packets[i].Data as Exception;

									}
									else if (packets[i].Kind == DataPacketKind.Snapshot)
									{
										Level2Snapshot l2s = packets[i].Data as Level2Snapshot;
										level2.SetSnapshot(l2s);

										var b = level2.Bids.Select(kv => new PriceItem(kv.Key, kv.Value))
											.OrderByDescending(k=> k.Price);

										var a = level2.Asks.Select(kv => new PriceItem(kv.Key, kv.Value))
											.OrderByDescending(k => k.Price);

										Level2Changed?.Invoke(b.ToArray(), a.ToArray());

									}
									else if (packets[i].Kind == DataPacketKind.Diff)
									{
										DiffData dd = packets[i].Data as DiffData;
										level2.AddDiff(dd);

										var b = level2.Bids.Select(kv => new PriceItem(kv.Key, kv.Value));
										var a = level2.Asks.Select(kv => new PriceItem(kv.Key, kv.Value));

										Level2Changed?.Invoke(b.ToArray(), a.ToArray());
									}
									else
										throw new InvalidProgramException("Ошибочный тип пакета");
								}

						}
						catch (Exception E)
						{
							Level2ChangedException?.Invoke(E);
						}
					}
				}, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
			}
		}
	}
}
