using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace DataSource
{
    using Data;
    using Data.Level2;
    using Queue;

    /// <summary>
    /// Изменение стакана
    /// </summary>
    public delegate void Level2DiffChangedHandler(
        IEnumerable<double> remove,
        IEnumerable<PriceItemEx> add,
        IEnumerable<PriceItemEx> replace);

    /// <summary>
    /// Исключение
    /// </summary>
    public delegate void Level2ChangedExceptionHandler(Exception E);

    public interface ILevel2Notify
    {
        /// <summary>
        /// Изменение стакана
        /// </summary>
        void OnLevel2DiffChanged(
            IEnumerable<double> remove,
            IEnumerable<PriceItemEx> add,
            IEnumerable<PriceItemEx> replace);

        /// <summary>
        /// Исключение
        /// </summary>
        void OnLevel2ChangedException(Exception E);
    }

    public interface IDataSource
    {
        /// <summary>
        /// Изменение стакана
        /// </summary>
        event Level2DiffChangedHandler OnLevel2DiffChanged;

        /// <summary>
        /// Исключение
        /// </summary>
        event Level2ChangedExceptionHandler OnLevel2ChangedException;

        Task Start(Level2 level2, IDataGet queue, CancellationTokenSource cts);
    }

    namespace Impl
    {
        public class DataSourceClient : IDataSource
        {
            /// <summary>
            /// Изменение стакана
            /// </summary>
            public event Level2DiffChangedHandler OnLevel2DiffChanged;

            /// <summary>
            /// Исключение
            /// </summary>
            public event Level2ChangedExceptionHandler OnLevel2ChangedException;

            public Task Start(Level2 level2, IDataGet queue, CancellationTokenSource cts)
            {
                return Task.Factory.StartNew(
                () =>
                {
                    while (true)
                    {
                        try
                        {
                            if (queue.Dequeue(out DataPacket[] packets))
                                for (var i = 0; i < packets.Length; i++)
                                {
                                    if (packets[i].Kind == DataPacketKind.Error)
                                    {
                                        Exception E = packets[i].Data as Exception;
                                        throw E;
                                    }
                                    else if (packets[i].Kind == DataPacketKind.Snapshot)
                                    {
                                        Level2Snapshot l2s = packets[i].Data as Level2Snapshot;
                                        IReadOnlyCollection<PriceItemEx> added = level2.SetSnapshot(l2s);
                                                                                                                                                                                                      
                                        OnLevel2DiffChanged?.Invoke(null, added, null);

                                    }
                                    else if (packets[i].Kind == DataPacketKind.Diff)
                                    {
                                        DiffData dd = packets[i].Data as DiffData;

                                        var (remove, add, replace) = level2.AddDiff(dd);

                                        OnLevel2DiffChanged?.Invoke(remove, add, replace);
                                    }
                                    else
                                        throw new InvalidProgramException("Ошибочный тип пакета");
                                }

                        }
                        catch (Exception E)
                        {
                            OnLevel2ChangedException?.Invoke(E);
                        }
                    }
                }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }
    }
}
