using System;
using System.Collections.Generic;
using System.Threading;


using Data.Level2;
using Remote;

namespace Queue
{
	/// <summary>
	/// Тип пакета
	/// </summary>
	public enum DataPacketKind
	{
		Error,
		Snapshot,
		Diff
	}

	/// <summary>
	/// Пакет данных
	/// </summary>
	public class DataPacket
	{
		public DataPacketKind Kind { get; private set; }
		public object Data { get; private set; }

		private DataPacket(DataPacketKind kind, object data)
		{
			Kind = kind;
			Data = data ?? throw new ArgumentNullException(nameof(data));
		}

		static public DataPacket New(Exception e)
		{
			return new DataPacket(DataPacketKind.Error, e);
		}

		static public DataPacket New(Level2Snapshot s)
		{
			return new DataPacket(DataPacketKind.Snapshot, s);
		}

		static public DataPacket New(DiffData dd)
		{
			return new DataPacket(DataPacketKind.Diff, dd);
		}
	}

	/// <summary>
	/// Получение данных из очереди
	/// </summary>
	public interface IDataGet
	{
		bool Dequeue(out DataPacket[] packets);
	}

	namespace Impl
	{
		/// <summary>
		/// Очередь с данными от сервера
		/// </summary>
		public class DataQueue : IDataAdd, IDataGet
		{
			private Queue<DataPacket> _queue = new Queue<DataPacket>();
			private object _lock = new object();
			private AutoResetEvent _event = new AutoResetEvent(false);
			private readonly CancellationToken _token;

			public DataQueue(CancellationToken token)
			{
				_token = token;
			}

			public void Enqueue(DataPacket packet)
			{
				lock (_lock)
					_queue.Enqueue(packet);

				_event.Set();
			}

			public void Enqueue(Level2Snapshot l2s)
			{
				Enqueue(DataPacket.New(l2s));
			}
			public void Enqueue(DiffData dd)
			{
				Enqueue(DataPacket.New(dd));
			}
			public void Enqueue(Exception E)
			{
				Enqueue(DataPacket.New(E));
			}

			public bool Dequeue(out DataPacket[] packets)
			{
				WaitHandle[] handles = new WaitHandle[] { _event, _token.WaitHandle };
				int w = WaitHandle.WaitAny(handles);

				if (w == 0)
				{
					lock (_lock)
					{
						packets = _queue.ToArray();
						_queue.Clear();
					}

					return true;
				}
				else
				{
					packets = null;
					return false;
				}
			}
		}
		
	}
}
