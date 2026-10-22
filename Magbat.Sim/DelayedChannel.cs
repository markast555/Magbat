using System.Collections.Concurrent;
using System.Diagnostics;

namespace Magbat.Sim;

/// <summary>
/// Канал доставки сообщений с фиксированной задержкой, сохраняющий порядок отправки
/// </summary>
public sealed class DelayedChannel<T> : IDisposable
{
    private readonly BlockingCollection<(long DueAt, T Message)> _queue = new();
    private readonly long _delayTicks;
    private readonly Action<T> _onDelivered;
    private readonly Func<T, int>? _measureSize;
    private readonly Thread _thread;
    // Меняются только в потоке доставки, поэтому Interlocked не нужен
    private long _bytes;
    private long _messages;

    /// <summary>
    /// Суммарный размер доставленных сообщений, байт. Читать после Dispose
    /// </summary>
    public long BytesDelivered => _bytes;

    /// <summary>
    /// Число доставленных сообщений. Читать после Dispose
    /// </summary>
    public long MessagesDelivered => _messages;

    /// <summary>
    /// Создаёт канал и запускает поток доставки
    /// </summary>
    /// <param name="delayMs">Задержка доставки каждого сообщения, мс</param>
    /// <param name="onDelivered">Метод получателя, вызывается в момент доставки</param>
    /// <param name="measureSize">
    /// Функция расчёта размера сообщения в байтах. Если null, байты не считаются
    /// </param>
    public DelayedChannel(int delayMs, Action<T> onDelivered, Func<T, int>? measureSize = null)
    {
        _delayTicks = delayMs * Stopwatch.Frequency / 1000;
        _onDelivered = onDelivered;
        _measureSize = measureSize;
        _thread = new Thread(Run) { IsBackground = true };
        _thread.Start();
    }

    /// <summary>
    /// Ставит сообщение в очередь на доставку
    /// </summary>
    /// <remarks>
    /// Не ожидает доставки: сообщение приходит получателю позже, в потоке доставки.
    /// После Dispose вызывать нельзя
    /// </remarks>
    /// <param name="message">Сообщение</param>
    public void Send(T message)
    {
        _queue.Add((Stopwatch.GetTimestamp() + _delayTicks, message));
    }

    /// <summary>
    /// Доставляет сообщения получателю по мере наступления срока
    /// </summary>
    private void Run()
    {
        foreach (var item in _queue.GetConsumingEnumerable())
        {
            // Одна очередь и одинаковая задержка у всех сообщений, поэтому порядок сохраняется
            WaitUntil(item.DueAt);
            _onDelivered(item.Message);

            _messages++;
            if (_measureSize != null)
                _bytes += _measureSize(item.Message);
        }
    }

    /// <summary>
    /// Ожидает наступления указанного момента
    /// </summary>
    /// <param name="dueAt">Момент доставки в тиках Stopwatch</param>
    private static void WaitUntil(long dueAt)
    {
        // Спит короткими отрезками, а на последних миллисекундах ждёт активно:
        // на Windows Sleep(1) может спать около 15 мс и пропустить срок
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
    /// Закрывает приём, дожидается доставки оставшихся сообщений и завершает поток
    /// </summary>
    public void Dispose()
    {
        _queue.CompleteAdding();
        _thread.Join();
        _queue.Dispose();
    }
}