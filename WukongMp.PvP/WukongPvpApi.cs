using System.Collections.Generic;
using System.Linq;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using WukongMp.Pvp.Common.ECS;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP;

public sealed class WukongPvpApi(EntityStore world) // TODO: Do not leak Friflo types
{
    public int LevelId => State?.LevelId ?? 0;

    public bool InPvP => State?.InPvP ?? false;

    public bool InPvpTournament => State?.InTournament ?? false;

    public bool ImmobilizeAllowed => State?.ImmobilizeAllowed ?? true;
    public bool PhantomRushAllowed => State?.PhantomRushAllowed ?? true;
    public bool GourdAllowed => State?.GourdAllowed ?? true;
    public bool ConsumablesAllowed => State?.ConsumablesAllowed ?? true;
    public int EnemiesNgPlusLevel => State?.EnemiesNgPlusLevel ?? 0;
    public int CurrentRound => State?.CurrentRound ?? 1;
    public int TournamentRounds => State?.TournamentRounds ?? 3;
    public bool AntiStallEnabled => State?.AntiStallEnabled ?? true;

    public IEnumerable<int> RoundWinners
    {
        get
        {
            if (State is { } state)
            {
                for (var i = 0; i < state.RoundWinnersCount; i++)
                {
                    yield return state.GetRoundWinners(i);
                }
            }
        }
    }

    public ReadyObject? PvpStateEntity { get; set; }

    private AreaId? CurrentArea => WukongApi.Sync.CurrentAreaId;

    public PvpStateComponent? State
    {
        get
        {
            if (!CurrentArea.HasValue)
                PvpStateEntity = null;

            if (!PvpStateEntity.HasValue && CurrentArea.HasValue)
            {
                var entity = world
                    .Query<PvpStateComponent>()
                    // .HasValue<InScopeComponent, Entity>(CurrentArea.Value.Entity)
                    .Entities.FirstOrDefault();
                PvpStateEntity = entity != default ? new ReadyObject(WukongApi.Sync, entity) : null; // TODO: This constructor has to be internal
            }

            return PvpStateEntity?.Get<PvpStateComponent>();
        }
    }
}