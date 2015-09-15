using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PlugNPayHub.Utils
{
    public class EventsMonitor<T>
    {
        private readonly ConcurrentDictionary<string, EventContainer> _events = new ConcurrentDictionary<string, EventContainer>();

        public void FireEvent(string id, T data)
        {
            id = id.ToLower();

            _events.GetOrAdd(id, new EventContainer()).Enqueue(new EventData(id, data));
        }

        public void Clear()
        {
            _events.Clear();
        }

        public async Task<EventData> WaitOneAsync(string id, int millisecondsTimeout)
        {
            id = id.ToLower();

            EventContainer eventContainer = _events.GetOrAdd(id, new EventContainer());

            try
            {
                return await eventContainer.TryDequeue(millisecondsTimeout);
            }
            finally
            {
                if (eventContainer?.Count == 0)
                    _events.TryRemove(id, out eventContainer);
            }
        }

        public async Task<EventData> WaitAnyAsync(string[] ids, int millisecondsTimeout)
        {
            Ensure.NotNull(ids, nameof(ids));

            Stopwatch sw = new Stopwatch();

            EventContainer risedEventContainer = null;
            string risedId = null;
            try
            {
                while (millisecondsTimeout > sw.Elapsed.TotalMilliseconds)
                {
                    foreach (string idH in ids)
                    {
                        string id = idH.ToLower();

                        EventContainer eventContainer = _events.GetOrAdd(id, new EventContainer());

                        EventData value = await eventContainer.TryDequeue(50);
                        if (value == null) continue;

                        risedEventContainer = eventContainer;
                        risedId = id;

                        return value;
                    }

                    await Task.Delay(100);
                }

                return null;
            }
            finally
            {
                if (risedEventContainer?.Count == 0 && risedId != null)
                    _events.TryRemove(risedId, out risedEventContainer);
            }
        }

        class EventContainer
        {
            private readonly ConcurrentQueue<EventData> _eventsQueue = new ConcurrentQueue<EventData>();
            private readonly SemaphoreSlim _wait = new SemaphoreSlim(0);

            public int Count => _eventsQueue.Count;

            public void Enqueue(EventData data)
            {
                _eventsQueue.Enqueue(data);
                _wait.Release();
            }

            public async Task<EventData> TryDequeue(int millisecondsTimeout)
            {
                await _wait.WaitAsync(millisecondsTimeout);

                EventData data;
                return _eventsQueue.TryDequeue(out data) ? data : null;
            }
        }

        public class EventData
        {
            public string Id { get; }
            public T Value { get; }
            public DateTime FiredTime { get; }

            public EventData(string id, T value)
            {
                Ensure.NotNull(id, nameof(id));

                Id = id;
                Value = value;
                FiredTime = DateTime.Now;
            }
        }

    }
}
