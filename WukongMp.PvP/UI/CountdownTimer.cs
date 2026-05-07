using System;
using System.Timers;

namespace WukongMp.PvP.UI;

public class CountdownTimer
{
    private int _remainingSeconds;
    private int _totalSeconds;
    private readonly Timer _timer;
    private Action? _callback;
    private Action<int, int>? _onTickCallback;

    public CountdownTimer(int minutes, int seconds)
    {
        _totalSeconds = minutes * 60 + seconds;
        _remainingSeconds = _totalSeconds;
        _timer = new Timer(1000);
        _timer.Elapsed += TimerElapsed;
    }

    public void SetTime(int minutes, int seconds)
    {
        _totalSeconds = minutes * 60 + seconds;
        Reset();
    }

    private void TimerElapsed(object sender, ElapsedEventArgs e)
    {
        if (_remainingSeconds > 0)
        {
            _remainingSeconds--;
            _onTickCallback?.Invoke(_remainingSeconds / 60, _remainingSeconds % 60);
        }
        else
        {
            Stop();
            _callback?.Invoke();
        }
    }

    public void Start(Action onFinishedCallback, Action<int, int> onTickCallback)
    {
        _timer.Start();
        _callback = onFinishedCallback;
        _onTickCallback = onTickCallback;
    }

    public void Stop()
    {
        _timer.Stop();
    }

    public void Reset()
    {
        Stop();
        _callback = null;
        _onTickCallback = null;
        _remainingSeconds = _totalSeconds;
    }
}