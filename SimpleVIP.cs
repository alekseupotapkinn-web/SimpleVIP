using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using System.Text.Json;

namespace SimpleVIP;

public sealed class SimpleVipConfig
{
    public List<string> SteamIds { get; set; } = new();
    public int BonusHealth { get; set; } = 50;
    public int BonusArmor { get; set; } = 100;
    public bool ShowVipTag { get; set; } = true;
}

public class SimpleVIP : BasePlugin
{
    public override string ModuleName => "SimpleVIP";
    public override string ModuleVersion => "1.0.4";
    public override string ModuleAuthor => "Lil Gun";
    public override string ModuleDescription =>
        "Simple VIP plugin for CounterStrikeSharp 1.0.374";

    private SimpleVipConfig Config = new();

    private string ConfigPath =>
        Path.Combine(ModuleDirectory, "config.json");

    public override void Load(bool hotReload)
    {
        LoadConfig();

        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);

        AddCommand("css_vip", "VIP information", VipCommand);
        AddCommand("css_vipid", "Show SteamID", VipIdCommand);
        AddCommand("css_reloadvip", "Reload VIP configuration", ReloadVipCommand);
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

        // Первая попытка после спавна
        Server.NextFrame(() =>
        {
            GiveVipBonus(player);
        });

        return HookResult.Continue;
    }

    private void GiveVipBonus(CCSPlayerController player)
    {
        if (!player.IsValid || player.PlayerPawn?.Value == null)
            return;

        var pawn = player.PlayerPawn.Value;

        // Armor уже работает, поэтому выдаём его сразу
        pawn.ArmorValue = Config.BonusArmor;

        // Первая установка HP
        Server.NextFrame(() =>
        {
            if (!player.IsValid || player.PlayerPawn?.Value == null)
                return;

            var p1 = player.PlayerPawn.Value;

            p1.Health = 150;
            p1.ArmorValue = Config.BonusArmor;

            // Вторая установка HP
            Server.NextFrame(() =>
            {
                if (!player.IsValid || player.PlayerPawn?.Value == null)
                    return;

                var p2 = player.PlayerPawn.Value;

                p2.Health = 150;
                p2.ArmorValue = Config.BonusArmor;

                // Третья установка HP
                Server.NextFrame(() =>
                {
                    if (!player.IsValid || player.PlayerPawn?.Value == null)
                        return;

                    var p3 = player.PlayerPawn.Value;

                    p3.Health = 150;
                    p3.ArmorValue = Config.BonusArmor;

                    if (Config.ShowVipTag)
                    {
                        player.PrintToChat(
                            $" \x04[VIP] \x01Вы получили: 150 HP / {Config.BonusArmor} Armor"
                        );
                    }
                });
            });
        });
    }

    private void VipCommand(
        CCSPlayerController? player,
        CommandInfo command
    )
    {
        if (player == null)
            return;

        if (!IsVip(player))
        {
            player.PrintToChat(
                " \x02[VIP] \x01У вас нет VIP."
            );

            return;
        }

        var pawn = player.PlayerPawn?.Value;

        if (pawn != null)
        {
            player.PrintToChat(
                $" \x04[VIP] \x01VIP активен! Сейчас: {pawn.Health} HP / {pawn.ArmorValue} Armor"
            );

            player.PrintToChat(
                $" \x04[VIP] \x01Бонус: +{Config.BonusHealth} HP / {Config.BonusArmor} Armor"
            );
        }
        else
        {
            player.PrintToChat(
                $" \x04[VIP] \x01VIP активен! Бонус: +{Config.BonusHealth} HP / {Config.BonusArmor} Armor"
            );
        }
    }

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
