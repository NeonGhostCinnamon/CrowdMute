using Dalamud.Configuration;
using System;

namespace CrowdMute;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    public int MuteAtPlayers { get; set; } = 80;
    public int UnmuteHysteresis { get; set; } = 3;
    public float Radius { get; set; } = 0f;
    public float UnmuteDelaySeconds { get; set; } = 3f;
    public bool IsMuted { get; set; } = false;
    public uint SavedVolume { get; set; } = 100;
}
