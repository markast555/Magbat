using System.Collections.Concurrent;
using System.Diagnostics;

namespace Magbat.Sim;

/// <summary>
/// Канал доставки сообщений с фиксированной задержкой. Сохраняет порядок сообщений
/// </summary>
public sealed class DelayedChannel<T> : IDisposable
{
    private readonly BlockingCollection<(long DueAt, T Message)> _queue = new();
    private readonly long _delayTicks;
    private readonly Action<T> _onDelivered;
    private readonly Func<T, int>? _measureSize;
    private readonly Thread _thread;
    private long _bytes;
    private long _messages;

    /// <summary>
    /// Сумма размеров доставленных сообщений. Читать после Dispose
    /// </summary>
    public long BytesDelivered => _bytes;

    /// <summary>
    /// Число доставленных сообщений. Читать после Dispose
    /// </summary>
    public long MessagesDelivered => _messages;

    public DelayedChannel(int delayMs, Action<T> onDelivered, Func<T, int>? measureSize = null)
    {
        _delayTicks = delayMs * Stopwatch.Frequency / 1000;
        _onDelivered = onDelivered;
        _measureSize = measureSize;
        _thread = new Thread(Run) { IsBackground = true };
        _thread.Start();
    }

    /// <summary>
    /// Ставит сообщение в очередь на доставку. Сразу возвращает управление
    /// </summary>
    /// <param name="message">Сообщение</param>
    public void Send(T message)
    {
        _queue.Add((Stopwatch.GetTimestamp() + _delayTicks, message));
    }

    private void Run()
    {
        foreach (var item in _queue.GetConsumingEnumerable())
        {
            WaitUntil(item.DueAt);
            _onDelivered(item.Message);

            _messages++;
            if (_measureSize != null)
                _bytes += _measureSize(item.Message);
        }
    }

    private static void WaitUntil(long dueAt)
    {
        while (true)
        {
            long remaining = dueAt - Stopwatch.GetTimestamp();
            if (remaining <= 0) return;

            double remainingMs = remaining * 1000.0 / Stopwatch.Frequency;
            if (remainingMs > 2) Thread.Sleep(1);
            else Thread.Yield();
        }
    }


    /// <summary>
    /// Закрывает приём, дожидается доставки оставшихся сообщений
    /// </summary>
    public void Dispose()
    {
        _queue.CompleteAdding();
        _thread.Join();
        _queue.Dispose();
    }
}