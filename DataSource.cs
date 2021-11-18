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

	public delegate void Level2ChangedHandler(IDictionary<double, double> bids, IDictionary<double, double> asks);
	public delegate void Level2ChangedExceptionHandler(Exception E);

	public interface ILevel2Notify
	{
		void Level2Changed(IDictionary<double, double> bids, IDictionary<double, double> asks);
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

										IDictionary<double, double> b = new Dictionary<double, double>(level2.Bids);

										IDictionary<double, double> a = new Dictionary<double, double>(level2.Asks);

										Level2Changed?.Invoke(b, a);

									}
									else if (packets[i].Kind == DataPacketKind.Diff)
									{
										DiffData dd = packets[i].Data as DiffData;
										level2.AddDiff(dd);

										IDictionary<double, double> b = new Dictionary<double, double>(level2.Bids);

										IDictionary<double, double> a = new Dictionary<double, double>(level2.Asks);

										Level2Changed?.Invoke(b, a);
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
