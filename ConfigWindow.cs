using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace CrowdMute;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly Configuration config;
    private readonly Action save;
    private readonly ITextureProvider textures;
    private readonly string iconPath;
    private readonly IFontAtlas fontAtlas;

    // Real-size fonts (re-rasterized at the right size, so they stay sharp)
    private const float BaseFontPx = 16f;
    private readonly Dictionary<float, IFontHandle> textFonts = new();
    private readonly Dictionary<float, IFontHandle> iconFonts = new();
    private bool collapsed;

    // Local working copies (floats so one slider routine handles ints and floats)
    private float muteAtPlayers;
    private float unmuteMargin;
    private float radius;
    private float unmuteDelay;

    // Layout (design units; multiplied by GlobalScale at draw time)
    private const float W = 440f;
    private const float H = 694f;
    private const float CW = 350f; // collapsed width
    private const float CH = 64f;  // collapsed height
    private const string GitHubUrl = "https://github.com/NeonGhostCinnamon/CrowdMute";
    private const string KofiUrl = "https://ko-fi.com/willocinnamon";

    // Colors
    private static readonly Vector4 Pink = new(0.97f, 0.42f, 0.72f, 1f);
    private static readonly Vector4 PinkHover = new(1f, 0.52f, 0.80f, 1f);
    private static readonly Vector4 White = new(0.95f, 0.95f, 0.98f, 1f);
    private static readonly Vector4 Dim = new(0.70f, 0.70f, 0.80f, 1f);
    private static readonly Vector4 WindowBg = new(0.17f, 0.16f, 0.24f, 1f);
    private static readonly Vector4 Border = new(1f, 1f, 1f, 0.14f);
    private static readonly Vector4 CardBg = new(1f, 1f, 1f, 0.035f);
    private static readonly Vector4 Separator = new(1f, 1f, 1f, 0.09f);
    private static readonly Vector4 InputBg = new(1f, 1f, 1f, 0.05f);
    private static readonly Vector4 TrackBg = new(1f, 1f, 1f, 0.14f);
    private static readonly Vector4 BtnBg = new(1f, 1f, 1f, 0.05f);
    private static readonly Vector4 BtnBgHover = new(1f, 1f, 1f, 0.12f);

    // Per-frame drawing state
    private ImDrawListPtr dl;
    private Vector2 origin;
    private float s = 1f;
    private bool sliderDirty;

    public ConfigWindow(Configuration config, Action save, ITextureProvider textures, string iconPath, IFontAtlas fontAtlas)
        : base("CrowdMute###CrowdMuteConfig",
               ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
               ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse |
               ImGuiWindowFlags.NoCollapse)
    {
        this.config = config;
        this.save = save;
        this.textures = textures;
        this.iconPath = iconPath;
        this.fontAtlas = fontAtlas;

        Size = ImGuiHelpers.ScaledVector2(W, H);
        SizeCondition = ImGuiCond.Always;

        RespectCloseHotkey = false;
        DisableWindowSounds = true;
        AllowBackgroundBlur = true;
        BgAlpha = 0.92f;

        SyncLocalState();
    }

    private void SyncLocalState()
    {
        muteAtPlayers = config.MuteAtPlayers;
        unmuteMargin = config.UnmuteMargin;
        radius = config.Radius;
        unmuteDelay = config.UnmuteDelaySeconds;
    }

    public new void Toggle()
    {
        if (!IsOpen)
            SyncLocalState();

        IsOpen = !IsOpen;
    }

    // ───────────────────────── Window style ─────────────────────────

    public override void PreDraw()
    {
        Size = ImGuiHelpers.ScaledVector2(collapsed ? CW : W, collapsed ? CH : H);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 18f * ImGuiHelpers.GlobalScale);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, C(WindowBg));
        ImGui.PushStyleColor(ImGuiCol.Border, C(Border));
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor(2);
        ImGui.PopStyleVar(3);
    }

    // ───────────────────────── Draw ─────────────────────────

    public override void Draw()
    {
        dl = ImGui.GetWindowDrawList();
        origin = ImGui.GetWindowPos();
        s = ImGuiHelpers.GlobalScale;

        if (collapsed)
        {
            DrawCollapsed();
            return;
        }

        DrawHeader();
        DrawCard();
        DrawFooter();
    }

    private void DrawHeader()
    {
        // Logo (no tile)
        Logo(14, 14, 76, FontAwesomeIcon.UsersSlash, 2.6f);

        // Title: "Crowd" white + "Mute" pink
        var crowdW = Measure("Crowd", 2.1f).X / s;
        Text(106, 24, "Crowd", White, 2.1f);
        Text(106 + crowdW, 24, "Mute", Pink, 2.1f);
        Text(108, 64, "A quieter Eorzea.", Dim);

        // Window buttons (top right)
        if (IconButton("##collapse", 372, 30, FontAwesomeIcon.ChevronDown, "Collapse"))
            collapsed = true;
        if (IconButton("##close", 412, 30, FontAwesomeIcon.Times, "Close"))
            IsOpen = false;

        // Pink underline with soft glow
        dl.AddLine(P(10, 106), P(W - 10, 106), C(A(Pink, 0.25f)), 6f * s);
        dl.AddLine(P(10, 106), P(W - 10, 106), C(Pink), 2.5f * s);
    }

    private void DrawCollapsed()
    {
        // Logo (no tile)
        Logo(8, 8, 48, FontAwesomeIcon.UsersSlash, 1.6f);

        // Title
        var crowdW = Measure("Crowd", 1.3f).X / s;
        TextMid(64, 32, "Crowd", White, 1.3f);
        TextMid(64 + crowdW, 32, "Mute", Pink, 1.3f);

        // Muting / Unmuted pill
        var muting = config.IsMuted;
        var col = muting ? Pink : Dim;
        var label = muting ? "Muting" : "Unmuted";
        const float px = 170f, pw = 100f, ph = 28f, py = 18f;

        dl.AddRectFilled(P(px, py), P(px + pw, py + ph), C(A(col, 0.14f)), ph / 2f * s);
        dl.AddRect(P(px, py), P(px + pw, py + ph), C(col), ph / 2f * s, ImDrawFlags.None, 1.5f * s);
        Icon(muting ? FontAwesomeIcon.VolumeMute : FontAwesomeIcon.VolumeUp, px + 20, py + ph / 2f, col, 0.9f);
        TextMid(px + 36, py + ph / 2f, label, col);

        // Expand / close
        if (IconButton("##expand", 296, 32, FontAwesomeIcon.ChevronUp, "Expand"))
            collapsed = false;
        if (IconButton("##close", 322, 32, FontAwesomeIcon.Times, "Close"))
            IsOpen = false;
    }

    /// <summary>Draws the CrowdMute logo PNG in a square; falls back to a font icon if the file is missing.</summary>
    private void Logo(float x, float y, float size, FontAwesomeIcon fallback, float fallbackScale)
    {
        var wrap = textures.GetFromFile(iconPath).GetWrapOrDefault();
        if (wrap != null)
            dl.AddImage(wrap.Handle, P(x, y), P(x + size, y + size));
        else
            Icon(fallback, x + size / 2f, y + size / 2f, Pink, fallbackScale);
    }

    private void DrawCard()
    {
        const float top = 120f, bottom = 618f;

        dl.AddRectFilled(P(10, top), P(W - 10, bottom), C(CardBg), 14f * s);
        dl.AddRect(P(10, top), P(W - 10, bottom), C(Border), 14f * s, ImDrawFlags.None, 1.2f * s);

        // Card header
        Icon(FontAwesomeIcon.Cog, 36, 144, White, 1.5f);
        Text(64, 128, "Mute Conditions", White, 1.2f);
        Text(64, 153, "Configure when CrowdMute should mute and unmute.", Dim);
        HLine(184);

        // Nearby players row
        var count = Plugin.LastCount;
        Icon(FontAwesomeIcon.Users, 44, 219.5f, Dim, 1.6f);
        Text(84, 190, "Nearby Players", Dim);
        var countText = count.ToString();
        var countW = Measure(countText, 2f).X / s;
        Text(84, 206, countText, count >= config.MuteAtPlayers ? Pink : White, 2f);
        var slashX = 84 + countW + 8;
        Text(slashX, 206, "/", Pink, 2f);
        Text(slashX + Measure("/ ", 2f).X / s, 206, config.MuteAtPlayers.ToString(), Dim, 2f);
        Text(28, 252, "Players within the specified radius.", Dim);

        // Divider between count and badge
        dl.AddLine(P(238, 202), P(238, 246), C(Separator), 1.5f * s);

        DrawBadge(282, 204, 122, 38);
        HLine(274);

        // Sliders
        const float rowH = 86f;
        var y = 274f;

        SliderRow("mute", "Mute when at least", y, ref muteAtPlayers, 1, 100, 1f, true, "players",
            v => config.MuteAtPlayers = (int)v);
        y += rowH; HLine(y);

        SliderRow("margin", "Unmute margin", y, ref unmuteMargin, 0, 50, 1f, true, "players",
            v => config.UnmuteMargin = (int)v);
        y += rowH; HLine(y);

        SliderRow("radius", "Radius", y, ref radius, 5, 100, 1f, false, "yalms",
            v => config.Radius = v);
        y += rowH; HLine(y);

        SliderRow("delay", "Unmute delay", y, ref unmuteDelay, 0, 30, 0.5f, false, "seconds",
            v => config.UnmuteDelaySeconds = v);
    }

    private void DrawBadge(float x, float y, float w, float h)
    {
        var muted = config.IsMuted;
        var col = muted ? Pink : Dim;

        dl.AddRectFilled(P(x, y), P(x + w, y + h), C(A(col, 0.14f)), h / 2f * s);
        dl.AddRect(P(x, y), P(x + w, y + h), C(col), h / 2f * s, ImDrawFlags.None, 2f * s);

        var label = muted ? "Muted" : "Unmuted";
        var labelW = Measure(label).X / s;
        const float iconW = 20f, gap = 10f;
        var startX = x + (w - (iconW + gap + labelW)) / 2f;

        Icon(muted ? FontAwesomeIcon.VolumeMute : FontAwesomeIcon.VolumeUp, startX + iconW / 2f, y + h / 2f, col, 1.1f);
        TextMid(startX + iconW + gap, y + h / 2f, label, col);
    }

    private void DrawFooter()
    {
        const float y = 632f, h = 48f;

        var enabled = config.Enabled;
        var label = enabled ? "DISABLE CROWDMUTE" : "ENABLE CROWDMUTE";
        var icon = enabled ? FontAwesomeIcon.PowerOff : FontAwesomeIcon.UsersSlash;

        var clicked = enabled
            ? FooterButton("##toggle", 12, y, 220, h, BtnBg, BtnBgHover, Pink, Pink, icon, label, false)
            : FooterButton("##toggle", 12, y, 220, h, Pink, PinkHover, A(White, 0.35f), White, icon, label, true);

        if (clicked)
        {
            config.Enabled = !enabled;
            save();
        }

        dl.AddLine(P(240, y + 4), P(240, y + h - 4), C(Separator), 1.5f * s);

        if (FooterButton("##kofi", 250, y, 82, h, BtnBg, BtnBgHover, Border, White,
                FontAwesomeIcon.Coffee, "Ko-fi", false, rainbow: true))
            Util.OpenLink(KofiUrl);

        if (FooterButton("##github", 340, y, 90, h, BtnBg, BtnBgHover, Border, White,
                FontAwesomeIcon.CodeBranch, "GitHub", false))
            Util.OpenLink(GitHubUrl);
    }

    // ───────────────────────── Slider row ─────────────────────────

    private void SliderRow(string id, string label, float top, ref float val, float min, float max,
        float step, bool isInt, string unit, Action<float> apply)
    {
        const float trackX = 28f, trackW = 230f;
        var cy = top + 50f;

        Text(28, top + 12, label, White);

        // ── Interaction: draggable track ──
        ImGui.SetCursorScreenPos(P(trackX - 10, cy - 12));
        ImGui.InvisibleButton($"##{id}_track", new Vector2((trackW + 20) * s, 24 * s));
        var hovered = ImGui.IsItemHovered();
        if (ImGui.IsItemActive())
        {
            var ratio = Math.Clamp((ImGui.GetMousePos().X - P(trackX, 0).X) / (trackW * s), 0f, 1f);
            var nv = min + ratio * (max - min);
            nv = Math.Clamp(MathF.Round(nv / step) * step, min, max);
            if (Math.Abs(nv - val) > 0.0001f)
            {
                val = nv;
                apply(val);
                sliderDirty = true;
            }
        }
        if (ImGui.IsItemDeactivated() && sliderDirty)
        {
            sliderDirty = false;
            save();
        }

        // ── Track visuals ──
        var fill = Math.Clamp((val - min) / (max - min), 0f, 1f);
        var fillX = trackX + trackW * fill;

        dl.AddRectFilled(P(trackX, cy - 3), P(trackX + trackW, cy + 3), C(TrackBg), 3f * s);
        if (fill > 0f)
            dl.AddRectFilled(P(trackX, cy - 3), P(fillX, cy + 3), C(Pink), 3f * s);

        var handle = P(fillX, cy);
        dl.AddCircleFilled(handle, (hovered ? 17f : 15f) * s, C(A(Pink, 0.22f)));
        dl.AddCircleFilled(handle, 11f * s, C(Pink));
        dl.AddCircle(handle, 11f * s, C(A(White, 0.7f)), 0, 1.5f * s);

        // Min / max labels
        Text(trackX, cy + 14, Fmt(min), Dim);
        var maxStr = Fmt(max);
        Text(trackX + trackW - Measure(maxStr).X / s, cy + 14, maxStr, Dim);

        // ── Editable value box ──
        var boxW = 76f * s;
        var boxH = 38f * s;
        var valStr = isInt ? ((int)val).ToString() : Fmt(val);
        var textSz = ImGui.CalcTextSize(valStr);
        var padX = Math.Max(4f * s, (boxW - textSz.X) / 2f);
        var padY = Math.Max(0f, (boxH - textSz.Y) / 2f);

        ImGui.SetCursorScreenPos(P(282, top + 30));
        ImGui.SetNextItemWidth(boxW);

        bool changed, committed;
        using (ImRaii.PushColor(ImGuiCol.FrameBg, InputBg))
        using (ImRaii.PushColor(ImGuiCol.FrameBgHovered, BtnBgHover))
        using (ImRaii.PushColor(ImGuiCol.FrameBgActive, BtnBgHover))
        using (ImRaii.PushColor(ImGuiCol.Border, Border))
        using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(padX, padY)))
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 10f * s))
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1.2f))
        {
            if (isInt)
            {
                var iv = (int)val;
                changed = ImGui.InputInt($"##{id}_input", ref iv, 0, 0);
                if (changed) val = iv;
            }
            else
            {
                var fv = val;
                changed = ImGui.InputFloat($"##{id}_input", ref fv, 0f, 0f, "%g");
                if (changed) val = fv;
            }
            committed = ImGui.IsItemDeactivatedAfterEdit();
        }

        if (changed)
        {
            val = Math.Clamp(val, min, max);
            apply(val);
        }
        if (committed)
            save();

        TextMid(366, top + 49, unit, Dim);
    }

    // ───────────────────────── Drawing helpers ─────────────────────────

    private bool FooterButton(string id, float x, float y, float w, float h,
        Vector4 fill, Vector4 fillHover, Vector4 border, Vector4 textCol,
        FontAwesomeIcon icon, string label, bool glow, bool rainbow = false)
    {
        ImGui.SetCursorScreenPos(P(x, y));
        var clicked = ImGui.InvisibleButton(id, new Vector2(w, h) * s);
        var hovered = ImGui.IsItemHovered();
        var active = ImGui.IsItemActive();

        var min = P(x, y);
        var max = P(x + w, y + h);

        if (glow)
            dl.AddRectFilled(min - new Vector2(4, 4) * s, max + new Vector2(4, 4) * s, C(A(Pink, hovered ? 0.30f : 0.20f)), 16f * s);

        var bg = active ? A(fill, 0.8f) : hovered ? fillHover : fill;
        dl.AddRectFilled(min, max, C(bg), 12f * s);
        dl.AddRect(min, max, C(border), 12f * s, ImDrawFlags.None, 1.5f * s);

        const float iconW = 20f, gap = 10f;
        var labelW = Measure(label).X / s;
        var startX = x + (w - (iconW + gap + labelW)) / 2f;
        Icon(icon, startX + iconW / 2f, y + h / 2f, textCol, 1.1f);
        if (rainbow)
            RainbowText(startX + iconW + gap, y + h / 2f, label);
        else
            TextMid(startX + iconW + gap, y + h / 2f, label, textCol);

        return clicked;
    }

    /// <summary>Text whose letters cycle through the rainbow over time.</summary>
    private void RainbowText(float x, float cy, string text)
    {
        var time = (float)ImGui.GetTime();
        var cx = x;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i].ToString();
            var hue = ((time * 0.4f - i * 0.12f) % 1f + 1f) % 1f;
            TextMid(cx, cy, ch, HsvToRgb(hue, 0.6f, 1f));
            cx += Measure(ch).X / s;
        }
    }

    private static Vector4 HsvToRgb(float h, float sat, float v)
    {
        var k = h * 6f;
        var i = (int)MathF.Floor(k) % 6;
        var f = k - MathF.Floor(k);
        var p = v * (1f - sat);
        var q = v * (1f - f * sat);
        var u = v * (1f - (1f - f) * sat);
        return i switch
        {
            0 => new Vector4(v, u, p, 1f),
            1 => new Vector4(q, v, p, 1f),
            2 => new Vector4(p, v, u, 1f),
            3 => new Vector4(p, q, v, 1f),
            4 => new Vector4(u, p, v, 1f),
            _ => new Vector4(v, p, q, 1f),
        };
    }

    private bool IconButton(string id, float cx, float cy, FontAwesomeIcon icon, string tooltip)
    {
        ImGui.SetCursorScreenPos(P(cx - 14, cy - 14));
        var clicked = ImGui.InvisibleButton(id, new Vector2(28, 28) * s);
        var hovered = ImGui.IsItemHovered();
        if (hovered)
            ImGui.SetTooltip(tooltip);
        Icon(icon, cx, cy, hovered ? White : Dim, 1.2f);
        return clicked;
    }

    private void HLine(float y) =>
        dl.AddLine(P(22, y), P(W - 22, y), C(Separator), 1f * s);

    /// <summary>Draws text with its top-left at (x, y) in design units.</summary>
    private void Text(float x, float y, string text, Vector4 col, float fontScale = 1f)
    {
        using var font = PushText(fontScale);
        dl.AddText(P(x, y), C(col), text);
    }

    /// <summary>Draws text vertically centered on cy.</summary>
    private void TextMid(float x, float cy, string text, Vector4 col, float fontScale = 1f)
    {
        var h = Measure(text, fontScale).Y / s;
        Text(x, cy - h / 2f, text, col, fontScale);
    }

    private void Icon(FontAwesomeIcon icon, float cx, float cy, Vector4 col, float fontScale = 1f)
    {
        var handle = GetFont(iconFonts, fontScale, true);
        if (!handle.Available) return;

        var str = icon.ToIconString();
        using var font = handle.Push();
        var size = ImGui.CalcTextSize(str);
        dl.AddText(P(cx, cy) - size / 2f, C(col), str);
    }

    private Vector2 Measure(string text, float fontScale = 1f)
    {
        using var font = PushText(fontScale);
        return ImGui.CalcTextSize(text);
    }

    private IDisposable? PushText(float fontScale) =>
        fontScale == 1f ? null : GetFont(textFonts, fontScale, false).Push();

    private IFontHandle GetFont(Dictionary<float, IFontHandle> cache, float fontScale, bool icon)
    {
        if (cache.TryGetValue(fontScale, out var handle))
            return handle;

        var px = BaseFontPx * fontScale;
        handle = icon
            ? fontAtlas.NewDelegateFontHandle(e => e.OnPreBuild(tk => tk.AddFontAwesomeIconFont(new() { SizePx = px })))
            : fontAtlas.NewDelegateFontHandle(e => e.OnPreBuild(tk => tk.AddDalamudDefaultFont(px)));
        cache[fontScale] = handle;
        return handle;
    }

    public void Dispose()
    {
        foreach (var h in textFonts.Values) h.Dispose();
        foreach (var h in iconFonts.Values) h.Dispose();
        textFonts.Clear();
        iconFonts.Clear();
    }

    private Vector2 P(float x, float y) => origin + new Vector2(x, y) * s;
    private static uint C(Vector4 v) => ImGui.ColorConvertFloat4ToU32(v);
    private static Vector4 A(Vector4 v, float a) => new(v.X, v.Y, v.Z, a);
    private static string Fmt(float v) => v.ToString("0.##");
}
