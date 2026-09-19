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
    public override string ModuleVersion => "1.0.1";
    public override string ModuleAuthor => "Lil Gun";
    public override string ModuleDescription =>
        "Simple VIP plugin for CounterStrikeSharp 1.0.374";

    private SimpleVipConfig Config = new();

    private string ConfigPath =>
        Path.Combine(ModuleDirectory, "config.json");

    public override void Load(bool hotReload)
    {
        LoadConfig();

        RegisterEventHandler<EventRoundStart>(OnRoundStart);

        AddCommand("css_vip", "VIP information", VipCommand);

        AddCommand("css_vipid", "Show your SteamID", VipIdCommand);

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

        var steamId = player.SteamID.ToString();

        return Config.SteamIds.Contains(
            steamId,
            StringComparer.OrdinalIgnoreCase
        );
    }

    private HookResult OnRoundStart(
        EventRoundStart @event,
        GameEventInfo info
    )
    {
        Server.NextFrame(() =>
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (
                    player == null ||
                    !player.IsValid ||
                    player.PlayerPawn?.Value == null
                )
                    continue;

                if (!IsVip(player))
                    continue;

                var pawn = player.PlayerPawn.Value;

                pawn.Health = Math.Min(
                    150,
                    pawn.Health + Config.BonusHealth
                );

                pawn.ArmorValue = Math.Max(
                    pawn.ArmorValue,
                    Config.BonusArmor
                );

                if (Config.ShowVipTag)
                {
                    player.PrintToChat(
                        $" \x04[VIP] \x01VIP бонус: +{Config.BonusHealth} HP / {Config.BonusArmor} armor"
                    );
                }
            }
        });

        return HookResult.Continue;
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

        player.PrintToChat(
            " \x04[VIP] \x01VIP активен!"
        );

        player.PrintToChat(
            $" \x04[VIP] \x01Бонус: +{Config.BonusHealth} HP / {Config.BonusArmor} armor"
        );
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
