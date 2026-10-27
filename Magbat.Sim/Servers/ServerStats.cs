using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Magbat.Sim;

/// <summary>
/// Статистика работы потока сервера
/// </summary>
public sealed class ServerStats
{
    private long _busyTicks;
    private long _allocatedBytes;
    private long _allocatedAtStart;
    private volatile int _osThreadId;

    /// <summary>Время активной работы потока без ожидания, мс</summary>
    public double BusyMs => Volatile.Read(ref _busyTicks) * 1000.0 / Stopwatch.Frequency;

    /// <summary>Память, выделенная потоком сервера, байт</summary>
    public long AllocatedBytes => Volatile.Read(ref _allocatedBytes);

    /// <summary>
    /// Запоминает поток сервера
    /// </summary>
    /// <remarks>Вызывать из этого потока</remarks>
    public void OnThreadStart()
    {
        _osThreadId = OperatingSystem.IsWindows() ? (int)GetCurrentThreadId() : 0;
        _allocatedAtStart = GC.GetAllocatedBytesForCurrentThread();
    }

    /// <summary>
    /// Добавляет время работы с указанного момента
    /// </summary>
    /// <remarks>Вызывать из потока сервера</remarks>
    public void AddBusy(long startedAt)
    {
        Volatile.Write(ref _busyTicks, _busyTicks + Stopwatch.GetTimestamp() - startedAt);
        Volatile.Write(ref _allocatedBytes, GC.GetAllocatedBytesForCurrentThread() - _allocatedAtStart);
    }

    /// <summary>
    /// Читает процессорное время потока сервера у операционной системы
    /// </summary>
    /// <returns>null, если определить не удалось</returns>
    public TimeSpan? ReadCpuTime()
    {
        int id = _osThreadId;
        if (id == 0) return null;

        try
        {
            using var process = Process.GetCurrentProcess();
            foreach (ProcessThread thread in process.Threads)
            {
                if (thread.Id == id)
                    return thread.TotalProcessorTime;
            }
        }
        catch (InvalidOperationException)
        {
        }
        return null;
    }

    [DllImport("kernel32.dll")]

    private static extern uint GetCurrentThreadId();
}