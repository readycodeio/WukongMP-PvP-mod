using System;

namespace WukongMp.PvP.GameEvents;

public readonly struct PvpEvent(PvpEventKind kind, int data = 0) : IEquatable<PvpEvent>
{
    public readonly PvpEventKind Kind = kind;
    public readonly int Data = data;

    public bool Equals(PvpEvent other)
        => Kind == other.Kind && Data == other.Data;

    public override bool Equals(object? obj)
        => obj is PvpEvent other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return ((int)Kind * 397) ^ Data;
        }
    }
}