using WukongMp.Sdk.Api;

namespace WukongMp.PvP.UI;

public class TimerController
{
    private CountdownTimer? _timer;
    private int _initialMinutes;
    private int _initialSeconds;

    public void SetTimer(int minutes, int seconds)
    {
        _initialMinutes = minutes;
        _initialSeconds = seconds;
        _timer = new CountdownTimer(minutes, seconds);
        WukongApi.Widgets.SetCountdownVisibility(true);
        WukongApi.Widgets.SetCountdownText(_initialMinutes, _initialSeconds);
    }

    public void StartTimer()
    {
        _timer?.Start(
            onFinishedCallback: OnTimerFinished,
            onTickCallback: OnTimerTick
        );
    }

    private void OnTimerTick(int minutes, int seconds)
    {
        WukongApi.Widgets.SetCountdownText(minutes, seconds);
    }

    private void OnTimerFinished()
    {
        WukongApi.Widgets.SetCountdownText(0, 0);
        WukongApi.Widgets.SetCountdownVisibility(false);
    }

    public void StopTimer()
    {
        _timer?.Stop();
        WukongApi.Widgets.SetCountdownVisibility(false);
    }

    public void ResetTimer()
    {
        _timer?.Reset();
        WukongApi.Widgets.SetCountdownVisibility(true);
        WukongApi.Widgets.SetCountdownText(_initialMinutes, _initialSeconds);
    }
}