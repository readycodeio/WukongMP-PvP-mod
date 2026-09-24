using ReadyM.SDK.Attributes;
using WukongMp.Sdk.Api;

namespace WukongMp.PvP.Services;

[Service]
public sealed partial class PlayerListRefresh(WidgetUpdates widgetUpdates)
{
    private float _lastUpdate;

    private void Start()
    {
        _lastUpdate = Time.Elapsed;
    }

    private void Update()
    {
        if (!WukongApi.Entities.CurrentArea.HasValue)
            return;

        if (Time.Elapsed - _lastUpdate < 1f)
            return;

        _lastUpdate = Time.Elapsed;

        widgetUpdates.RefreshPlayerLists();
        widgetUpdates.RefreshWidgets();
    }
}