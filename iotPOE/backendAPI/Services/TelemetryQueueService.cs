using System.Globalization;
using backendAPI.Models;

//Emeris School of Computer Science, 2025
namespace backendAPI.Services
{
    // what the status endpoint sends back, used to show the queues working
    public record QueueStatus(
        int NormalWaiting,
        int PriorityWaiting,
        long NormalEnqueued,
        long PriorityEnqueued,
        long NormalProcessed,
        long PriorityProcessed,
        List<string> RecentEvents);

    // holds the two queues for incoming telemetry
    // registered as a SINGLETON in Program.cs so every request and the background worker share the same queues
    public class TelemetryQueueService
    {
        // standard stream: first in, first out (Microsoft Learn, 2026)
        private readonly Queue<TelemetryPacket<double>> _normalQueue = new();

        // critical readings: the LOWEST priority number comes out first (Microsoft Learn, 2026)
        // the priority is the negative of how far past the limit the reading is, so the worst reading is dequeued first
        private readonly PriorityQueue<TelemetryPacket<double>, int> _priorityQueue = new();

        // the last 20 things that happened, kept in a Queue so the oldest one drops off
        private readonly Queue<string> _recentEvents = new();

        // requests and the worker use the queues at the same time, so every access is inside the lock
        private readonly object _lock = new();

        private long _normalEnqueued;
        private long _priorityEnqueued;
        private long _normalProcessed;
        private long _priorityProcessed;

        public void EnqueueNormal(TelemetryPacket<double> packet)
        {
            lock (_lock)
            {
                _normalQueue.Enqueue(packet);
                _normalEnqueued++;
            }
        }

        public void EnqueuePriority(TelemetryPacket<double> packet, int priority)
        {
            lock (_lock)
            {
                _priorityQueue.Enqueue(packet, priority);
                _priorityEnqueued++;
            }
        }

        // takes the most critical waiting reading, returns false when there is none
        public bool TryDequeuePriority(out TelemetryPacket<double>? packet)
        {
            lock (_lock)
            {
                if (_priorityQueue.TryDequeue(out packet, out int priority))
                {
                    _priorityProcessed++;
                    AddEvent("PRIORITY " + packet.SensorId + " = " + FormatValue(packet.Value) +
                             " (" + (-priority) + "% past the limit) processed immediately");
                    return true;
                }

                return false;
            }
        }

        // takes up to maxCount readings from the standard queue, oldest first
        public List<TelemetryPacket<double>> DequeueNormalBatch(int maxCount)
        {
            lock (_lock)
            {
                var batch = new List<TelemetryPacket<double>>();

                while (batch.Count < maxCount && _normalQueue.Count > 0)
                {
                    batch.Add(_normalQueue.Dequeue());
                }

                return batch;
            }
        }

        public void RecordNormalProcessed(int count)
        {
            lock (_lock)
            {
                _normalProcessed += count;
                AddEvent("NORMAL batch of " + count + " reading(s) saved by the worker");
            }
        }

        public QueueStatus GetStatus()
        {
            lock (_lock)
            {
                return new QueueStatus(
                    _normalQueue.Count,
                    _priorityQueue.Count,
                    _normalEnqueued,
                    _priorityEnqueued,
                    _normalProcessed,
                    _priorityProcessed,
                    _recentEvents.ToList());
            }
        }

        // only called from inside the lock
        private void AddEvent(string message)
        {
            _recentEvents.Enqueue("[" + DateTime.UtcNow.ToString("HH:mm:ss.fff") + "] " + message);

            while (_recentEvents.Count > 20)
            {
                _recentEvents.Dequeue();
            }
        }

        private static string FormatValue(double value)
        {
            return value.ToString("F1", CultureInfo.InvariantCulture);
        }
    }
}

/** 
References
Microsoft Learn, 2026. Queue<T> Class and PriorityQueue<TElement,TPriority> Class. (Version 10.0) [Source Code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.queue-1> [Accessed 16 October 2026]
Microsoft Learn, 2026. PriorityQueue<TElement,TPriority> Class.(Version 10.0) [Source Code] Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2> [Accessed 16 October 2026] 
*/