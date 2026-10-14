using Dalamud.Configuration;
using System;

namespace CrowdMute;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool Enabled { get; set; } = true;

    /// <summary>Mute when at least this many other players are within <see cref="Radius"/>.</summary>
    public int MuteAtPlayers { get; set; } = 15;

    /// <summary>Unmute once the count drops below MuteAtPlayers minus this (prevents flapping).</summary>
    public int UnmuteHysteresis { get; set; } = 3;

    /// <summary>Only count players within this many yalms. 0 = no limit (everyone the game has loaded).</summary>
    public float Radius { get; set; } = 0f;

    /// <summary>Seconds the area must stay below the unmute level before volume is restored.</summary>
    public float UnmuteDelaySeconds { get; set; } = 3f;

    // Persisted so a crash while muted doesn't leave your ambient volume at 0.
    public bool IsMuted { get; set; } = false;
    public uint SavedVolume { get; set; } = 100;
}
