using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;
using System.Numerics;

namespace CrowdMute;

public sealed class ConfigWindow : Window
{
    private readonly Configuration config;
    private readonly Action save;

    public ConfigWindow(Configuration config, Action save) : base("Crowd Mute")
    {
        this.config = config;
        this.save = save;
        Size = new Vector2(380, 230);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var enabled = config.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled)) { config.Enabled = enabled; save(); }

        var muteAt = config.MuteAtPlayers;
        if (ImGui.SliderInt("Mute at this many players", ref muteAt, 2, 100)) { config.MuteAtPlayers = muteAt; save(); }

        var hyst = config.UnmuteHysteresis;
        if (ImGui.SliderInt("Unmute margin", ref hyst, 0, 20)) { config.UnmuteHysteresis = hyst; save(); }

        var radius = config.Radius;
        if (ImGui.SliderFloat("Radius (yalms, 0 = any)", ref radius, 0f, 100f)) { config.Radius = radius; save(); }

        var delay = config.UnmuteDelaySeconds;
        if (ImGui.SliderFloat("Unmute delay (s)", ref delay, 0f, 15f)) { config.UnmuteDelaySeconds = delay; save(); }

        ImGui.Separator();
        ImGui.TextUnformatted($"Players counted now: {Plugin.LastCount}");
        ImGui.TextUnformatted(config.IsMuted ? "Status: ambient muted" : "Status: ambient normal");
    }
}
