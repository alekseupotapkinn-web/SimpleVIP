using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using System.Text.Json;

namespace SimpleVIP;

public sealed class SimpleVipConfig
{
    public List<string> SteamIds { get; set; } = new();

    public int BonusHealth { get; set; } = 50;

    public int BonusArmor { get; set; } = 100;

    public bool ShowVipTag { get; set; } = true;

    // Скорость: 1.20 = +20%
    public float SpeedMultiplier { get; set; } = 1.20f;

    // Гравитация: 0.80 = более высокий прыжок
    public float JumpGravity { get; set; } = 0.80f;

    // Стандартная скорость CS2
    public float BaseSpeed { get; set; } = 260.0f;
}

public class SimpleVIP : BasePlugin
{
    public override string ModuleName => "SimpleVIP";

    public override string ModuleVersion => "1.0.5";

    public override string ModuleAuthor => "Lil Gun";

    public override string ModuleDescription =>
        "Simple VIP plugin for CounterStrikeSharp 1.0.374";

    private SimpleVipConfig Config = new();

    private string ConfigPath =>
        Path.Combine(ModuleDirectory, "config.json");

    // Запоминаем включены ли бонусы у каждого VIP
    private readonly Dictionary<ulong, bool> SpeedEnabled = new();

    private readonly Dictionary<ulong, bool> JumpEnabled = new();

    public override void Load(bool hotReload)
    {
        LoadConfig();

        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);

        AddCommand(
            "css_vip",
            "Open VIP menu",
            VipCommand
        );

        AddCommand(
            "css_vipid",
            "Show SteamID",
            VipIdCommand
        );

        AddCommand(
            "css_reloadvip",
            "Reload VIP configuration",
            ReloadVipCommand
        );
    }

    private void LoadConfig()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                File.WriteAllText(
                    ConfigPath,
                    JsonSerializer.Serialize(
                        Config,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        }
                    )
                );

                return;
            }

            Config =
                JsonSerializer.Deserialize<SimpleVipConfig>(
                    File.ReadAllText(ConfigPath)
                ) ?? new SimpleVipConfig();
        }
        catch
        {
            Config = new SimpleVipConfig();
        }
    }

    private bool IsVip(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid)
            return false;

        string steamId = player.SteamID.ToString();

        return Config.SteamIds.Contains(
            steamId,
            StringComparer.OrdinalIgnoreCase
        );
    }

    private HookResult OnPlayerSpawn(
        EventPlayerSpawn @event,
        GameEventInfo info
    )
    {
        var player = @event.Userid;

        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (!IsVip(player))
            return HookResult.Continue;

        // Если игрок ещё не выбирал настройки,
        // включаем бонусы по умолчанию.
        if (!SpeedEnabled.ContainsKey(player.SteamID))
            SpeedEnabled[player.SteamID] = true;

        if (!JumpEnabled.ContainsKey(player.SteamID))
            JumpEnabled[player.SteamID] = true;

        Server.NextFrame(() =>
        {
            GiveVipBonus(player);
        });

        return HookResult.Continue;
    }

    private void GiveVipBonus(CCSPlayerController player)
    {
        if (!player.IsValid ||
            player.PlayerPawn?.Value == null)
            return;

        var pawn = player.PlayerPawn.Value;

        // ------------------------------------------------
        // HP
        // ------------------------------------------------

        pawn.Health = 150;

        // ------------------------------------------------
        // ARMOR
        // ------------------------------------------------

        pawn.ArmorValue = Config.BonusArmor;

        // ------------------------------------------------
        // SPEED
        // ------------------------------------------------

        if (pawn.MovementServices != null)
        {
            if (SpeedEnabled.TryGetValue(
                    player.SteamID,
                    out bool speedEnabled) &&
                speedEnabled)
            {
                pawn.MovementServices.Maxspeed =
                    Config.BaseSpeed * Config.SpeedMultiplier;
            }
            else
            {
                pawn.MovementServices.Maxspeed =
                    Config.BaseSpeed;
            }
        }

        // ------------------------------------------------
        // JUMP
        // ------------------------------------------------

        if (JumpEnabled.TryGetValue(
                player.SteamID,
                out bool jumpEnabled) &&
            jumpEnabled)
        {
            pawn.GravityScale = Config.JumpGravity;
        }
        else
        {
            pawn.GravityScale = 1.0f;
        }

        // ------------------------------------------------
        // Повторно устанавливаем HP/Armor после спавна
        // ------------------------------------------------

        Server.NextFrame(() =>
        {
            if (!player.IsValid ||
                player.PlayerPawn?.Value == null)
                return;

            var p = player.PlayerPawn.Value;

            p.Health = 150;
            p.ArmorValue = Config.BonusArmor;

            if (p.MovementServices != null)
            {
                if (SpeedEnabled.TryGetValue(
                        player.SteamID,
                        out bool speed) &&
                    speed)
                {
                    p.MovementServices.Maxspeed =
                        Config.BaseSpeed * Config.SpeedMultiplier;
                }
                else
                {
                    p.MovementServices.Maxspeed =
                        Config.BaseSpeed;
                }
            }

            if (JumpEnabled.TryGetValue(
                    player.SteamID,
                    out bool jump) &&
                jump)
            {
                p.GravityScale = Config.JumpGravity;
            }
            else
            {
                p.GravityScale = 1.0f;
            }

            if (Config.ShowVipTag)
            {
                player.PrintToChat(
                    $" \x04[VIP] \x01150 HP / {Config.BonusArmor} Armor"
                );

                if (SpeedEnabled[player.SteamID])
                {
                    player.PrintToChat(
                        $" \x04[VIP] \x01Скорость: +20%"
                    );
                }

                if (JumpEnabled[player.SteamID])
                {
                    player.PrintToChat(
                        $" \x04[VIP] \x01Высокий прыжок: ВКЛ"
                    );
                }
            }
        });
    }

    // ====================================================
    // VIP MENU
    // ====================================================

    private void VipCommand(
        CCSPlayerController? player,
        CommandInfo command
    )
    {
        if (player == null ||
            !player.IsValid)
            return;

        if (!IsVip(player))
        {
            player.PrintToChat(
                " \x02[VIP] \x01У вас нет VIP."
            );

            return;
        }

        OpenVipMenu(player);
    }

    private void OpenVipMenu(
        CCSPlayerController player
    )
    {
        var menu = new ChatMenu("VIP MENU");

        bool speed =
            SpeedEnabled.TryGetValue(
                player.SteamID,
                out bool speedValue
            )
                ? speedValue
                : true;

        bool jump =
            JumpEnabled.TryGetValue(
                player.SteamID,
                out bool jumpValue
            )
                ? jumpValue
                : true;

        // ------------------------------------------------
        // STATUS
        // ------------------------------------------------

        menu.AddMenuOption(
            "VIP статус",
            (p, option) =>
            {
                p.PrintToChat(
                    " \x04[VIP] \x01Ваш VIP активен."
                );

                p.PrintToChat(
                    $" \x04[VIP] \x01HP: 150 | Armor: {Config.BonusArmor}"
                );

                p.PrintToChat(
                    $" \x04[VIP] \x01Скорость: {(SpeedEnabled[p.SteamID] ? "ВКЛ" : "ВЫКЛ")}"
                );

                p.PrintToChat(
                    $" \x04[VIP] \x01Высокий прыжок: {(JumpEnabled[p.SteamID] ? "ВКЛ" : "ВЫКЛ")}"
                );

                OpenVipMenu(p);
            }
        );

        // ------------------------------------------------
        // SPEED
        // ------------------------------------------------

        menu.AddMenuOption(
            speed
                ? "Выключить скорость"
                : "Включить скорость",

            (p, option) =>
            {
                SpeedEnabled[p.SteamID] =
                    !SpeedEnabled[p.SteamID];

                ApplyMovementBonuses(p);

                p.PrintToChat(
                    SpeedEnabled[p.SteamID]
                        ? " \x04[VIP] \x01Увеличенная скорость: ВКЛ"
                        : " \x04[VIP] \x01Увеличенная скорость: ВЫКЛ"
                );

                OpenVipMenu(p);
            }
        );

        // ------------------------------------------------
        // JUMP
        // ------------------------------------------------

        menu.AddMenuOption(
            jump
                ? "Выключить высокий прыжок"
                : "Включить высокий прыжок",

            (p, option) =>
            {
                JumpEnabled[p.SteamID] =
                    !JumpEnabled[p.SteamID];

                ApplyMovementBonuses(p);

                p.PrintToChat(
                    JumpEnabled[p.SteamID]
                        ? " \x04[VIP] \x01Высокий прыжок: ВКЛ"
                        : " \x04[VIP] \x01Высокий прыжок: ВЫКЛ"
                );

                OpenVipMenu(p);
            }
        );

        // ------------------------------------------------
        // RE-APPLY BONUSES
        // ------------------------------------------------

        menu.AddMenuOption(
            "Применить VIP бонусы",

            (p, option) =>
            {
                GiveVipBonus(p);

                p.PrintToChat(
                    " \x04[VIP] \x01VIP бонусы применены."
                );

                OpenVipMenu(p);
            }
        );

        // ------------------------------------------------
        // CLOSE
        // ------------------------------------------------

        menu.AddMenuOption(
            "Закрыть",
            (p, option) =>
            {
                MenuManager.CloseActiveMenu(p);
            }
        );

        MenuManager.OpenChatMenu(
            player,
            menu
        );
    }

    // ====================================================
    // APPLY MOVEMENT
    // ====================================================

    private void ApplyMovementBonuses(
        CCSPlayerController player
    )
    {
        if (!player.IsValid ||
            player.PlayerPawn?.Value == null)
            return;

        var pawn = player.PlayerPawn.Value;

        if (pawn.MovementServices != null)
        {
            bool speed =
                SpeedEnabled.TryGetValue(
                    player.SteamID,
                    out bool speedValue
                ) && speedValue;

            pawn.MovementServices.Maxspeed =
                speed
                    ? Config.BaseSpeed * Config.SpeedMultiplier
                    : Config.BaseSpeed;
        }

        bool jump =
            JumpEnabled.TryGetValue(
                player.SteamID,
                out bool jumpValue
            ) && jumpValue;

        pawn.GravityScale =
            jump
                ? Config.JumpGravity
                : 1.0f;
    }

    // ====================================================
    // STEAM ID
    // ====================================================

    private void VipIdCommand(
        CCSPlayerController? player,
        CommandInfo command
    )
    {
        if (player == null)
            return;

        player.PrintToChat(
            $" \x04[VIP] \x01Ваш SteamID64: {player.SteamID}"
        );
    }

    // ====================================================
    // RELOAD CONFIG
    // ====================================================

    private void ReloadVipCommand(
        CCSPlayerController? player,
        CommandInfo command
    )
    {
        LoadConfig();

        player?.PrintToChat(
            " \x04[VIP] \x01Конфигурация перезагружена."
        );
    }
}
