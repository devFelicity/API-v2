using System.Timers;
using Timer = System.Timers.Timer;

namespace API.Util;

public class TimedDictionary<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue?> _dictionary = new();
    private readonly Dictionary<TKey, DateTime> _expirationTimes = new();
    private readonly Timer _timer = new();

    public TimedDictionary(TimeSpan expirationTime)
    {
        _timer.Interval = expirationTime.TotalMilliseconds;
        _timer.Elapsed += TimerElapsed;
        _timer.Start();
    }

    public void Add(TKey key, TValue? value)
    {
        lock (_dictionary)
        {
            if (!_dictionary.TryAdd(key, value))
            {
                _dictionary[key] = value;
                _expirationTimes[key] = DateTime.Now.AddMilliseconds(_timer.Interval);
            }
            else
            {
                _expirationTimes.Add(key, DateTime.Now.AddMilliseconds(_timer.Interval));
            }
        }
    }

    public void Remove(TKey key)
    {
        lock (_dictionary)
        {
            _dictionary.Remove(key);
            _expirationTimes.Remove(key);
        }
    }

    public bool TryGetValue(TKey key, out TValue? value)
    {
        lock (_dictionary)
        {
            if (_expirationTimes.TryGetValue(key, out var expirationTime) && expirationTime > DateTime.Now)
            {
                value = _dictionary[key];
                return true;
            }

            value = default;
            return false;
        }
    }

    private void TimerElapsed(object? sender, ElapsedEventArgs e)
    {
        lock (_dictionary)
        {
            var keysToRemove = _expirationTimes.Where(pair => pair.Value <= DateTime.Now).Select(pair => pair.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _dictionary.Remove(key);
                _expirationTimes.Remove(key);
            }
        }
    }
}
