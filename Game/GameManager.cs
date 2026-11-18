using PokemonAdventure.Data;
using PokemonAdventure.Models;
using PokemonAdventure.Systems;

namespace PokemonAdventure.Game;

public sealed class GameManager(Random? random = null)
{
    private readonly Random _random = random ?? new();
    private string? _savedSnapshot;

    public void Run()
    {
        while (true)
        {
            ConsoleUi.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("   TEXT-BASED POKÉMON ADVENTURE RPG");
            Console.WriteLine("              BETA 1.1");
            Console.WriteLine("========================================");
            Console.WriteLine("1. New Game");
            Console.WriteLine($"2. Load Game{(SaveSystem.SaveExists() ? string.Empty : " (no save found)")}");
            Console.WriteLine("3. Exit");
            Console.WriteLine($"4. Sound: {(AudioSystem.Enabled ? "ON" : "OFF")}");
            Console.Write("Choose: ");
            switch (ConsoleUi.ReadChoice(1, 4))
            {
                case 1:
                    MainLoop(NewGame());
                    break;
                case 2:
                    if (LoadGame() is { } player) MainLoop(player);
                    break;
                case 3:
                    return;
                case 4:
                    ToggleSound();
                    break;
            }
        }
    }

    private Player NewGame()
    {
        ConsoleUi.Clear();
        Console.WriteLine("Professor: Welcome to the world of Pokémon!");
        Console.Write("What is your name? ");
        var name = ConsoleUi.ReadLine().Trim();
        while (string.IsNullOrWhiteSpace(name))
        {
            Console.Write("Please enter your name: ");
            name = ConsoleUi.ReadLine().Trim();
        }
        var player = new Player { Name = name, SoundEnabled = AudioSystem.Enabled };
        _savedSnapshot = null;
        Console.WriteLine($"\nNice to meet you, {name}. Choose your starter:");
        Console.WriteLine("1. Bulbasaur (Grass)");
        Console.WriteLine("2. Charmander (Fire)");
        Console.WriteLine("3. Squirtle (Water)");
        var starterId = ConsoleUi.ReadChoice(1, 3) switch
        {
            1 => "bulbasaur",
            2 => "charmander",
            _ => "squirtle"
        };
        var starter = Pokemon.Create(starterId, 5);
        player.Party.Add(starter);
        player.RegisterCaughtSpecies(starterId);
        Console.WriteLine($"You received {starter.Name}! Route 1 is waiting for you.");
        AudioSystem.Play(SoundEffect.Caught);
        ConsoleUi.Pause();
        return player;
    }

    private Player? LoadGame()
    {
        var loaded = SaveSystem.Load();
        Console.WriteLine(loaded.Message);
        if (loaded.Player is { } player)
        {
            _savedSnapshot = loaded.RecoveredBackup ? null : SaveSystem.Snapshot(player);
            AudioSystem.Enabled = player.SoundEnabled;
            Console.WriteLine($"Welcome back, {player.Name}!");
        }
        ConsoleUi.Pause();
        return loaded.Player;
    }

    private void MainLoop(Player player)
    {
        var managementMenu = new PokemonManagementMenu(player);
        while (true)
        {
            ConsoleUi.Clear();
            Console.WriteLine("================================");
            Console.WriteLine("PALLET TOWN — BETA 1.1");
            Console.WriteLine($"Trainer: {player.Name} | Party: {player.Party.Count}/{Player.MaxPartySize} | Badges: {player.Badges.Count}");
            Console.WriteLine("================================");
            Console.WriteLine("1. Explore");
            Console.WriteLine("2. Party");
            Console.WriteLine("3. Pokédex");
            Console.WriteLine("4. Bag");
            Console.WriteLine("5. Pokémon Center");
            Console.WriteLine("6. Save Game");
            Console.WriteLine("7. Return to Title");
            Console.WriteLine("8. Pewter City Gym");
            Console.WriteLine("9. Pokémon Storage");
            Console.WriteLine($"10. Sound: {(player.SoundEnabled ? "ON" : "OFF")}");
            Console.Write("Choose: ");
            switch (ConsoleUi.ReadChoice(1, 10))
            {
                case 1:
                    Explore(player);
                    break;
                case 2:
                    managementMenu.ShowParty();
                    break;
                case 3:
                    ShowPokedex(player);
                    break;
                case 4:
                    ShowBag(player);
                    break;
                case 5:
                    HealParty(player);
                    break;
                case 6:
                    SaveGame(player);
                    break;
                case 7:
                    if (ConfirmReturnToTitle(player))
                    {
                        AudioSystem.StopAll();
                        return;
                    }
                    break;
                case 8:
                    ChallengeGym(player);
                    break;
                case 9:
                    managementMenu.ShowStorage();
                    break;
                case 10:
                    ToggleSound(player);
                    break;
            }
        }
    }

    private void Explore(Player player)
    {
        ConsoleUi.Clear();
        if (!player.HasUsablePokemon())
        {
            Console.WriteLine("Visit the Pokémon Center first.");
            ConsoleUi.Pause();
            return;
        }
        Console.WriteLine("1. Route 1 (Pidgey, Rattata)");
        Console.WriteLine(ExplorationSystem.CanExploreRoute(player, 2)
            ? "2. Route 2 (Caterpie, Weedle, Pidgey, Pikachu)"
            : "2. Route 2 (requires Boulder Badge)");
        Console.WriteLine("0. Back");
        var route = ConsoleUi.ReadChoice(0, 2);
        if (route == 0) return;
        if (!ExplorationSystem.CanExploreRoute(player, route))
        {
            Console.WriteLine("Defeat Brock to unlock Route 2.");
            ConsoleUi.Pause();
            return;
        }
        switch (ExplorationSystem.RollEvent(_random))
        {
            case ExplorationEvent.WildPokemon:
                var wildPokemon = route == 1
                    ? GameData.CreateRouteOneEncounter(_random)
                    : GameData.CreateRouteTwoEncounter(_random);
                new BattleSystem(player, _random).StartWildBattle(wildPokemon,
                    route == 1 ? GameData.RouteOneExperienceBonus : 0);
                break;
            case ExplorationEvent.Item:
                var (item, amount) = ExplorationSystem.FindItem(player, _random);
                Console.WriteLine($"You found {item} ×{amount}! Total: {player.Bag[item]}.");
                AudioSystem.Play(SoundEffect.ItemFound);
                ConsoleUi.Pause();
                break;
            default:
                Console.WriteLine("You found nothing this time.");
                ConsoleUi.Pause();
                break;
        }
    }

    private void ChallengeGym(Player player)
    {
        ConsoleUi.Clear();
        if (player.Badges.Contains(GameData.BoulderBadge))
        {
            Console.WriteLine("You already defeated Brock and earned the Boulder Badge.");
            ConsoleUi.Pause();
            return;
        }
        if (!player.HasUsablePokemon())
        {
            Console.WriteLine("Heal your party before challenging the Gym.");
            ConsoleUi.Pause();
            return;
        }
        var team = GameData.CreateBrockTeam();
        var lineup = string.Join(", ", team.Select(pokemon => $"{pokemon.Name} Lv. {pokemon.Level}"));
        Console.WriteLine($"PEWTER CITY GYM — Brock: {lineup}");
        Console.WriteLine("1. Challenge  0. Back");
        if (ConsoleUi.ReadChoice(0, 1) == 0) return;
        if (new BattleSystem(player, _random).StartTrainerBattle("Brock", team) &&
            ExplorationSystem.AwardBrockVictory(player))
        {
            Console.WriteLine("You received the Boulder Badge, 5 Poké Balls and 2 Potions!");
            Console.WriteLine("Route 2 is now open.");
            AudioSystem.Play(SoundEffect.ItemReceived);
            ConsoleUi.Pause();
        }
    }

    private static void ShowPokedex(Player player)
    {
        ConsoleUi.Clear();
        var totalSpecies = GameData.AllSpecies.Count;
        Console.WriteLine($"POKÉDEX — Seen: {player.SeenSpecies.Count}/{totalSpecies}, Caught: {player.CaughtSpecies.Count}/{totalSpecies}");
        foreach (var species in GameData.AllSpecies.OrderBy(s => s.Name))
        {
            var marker = player.CaughtSpecies.Contains(species.Id) ? "Caught"
                : player.SeenSpecies.Contains(species.Id) ? "Seen" : "-----";
            Console.WriteLine($"[{marker}] {species.Name}");
        }
        ConsoleUi.Pause();
    }

    private static void ShowBag(Player player)
    {
        ConsoleUi.Clear();
        Console.WriteLine("BAG");
        foreach (var item in player.Bag) Console.WriteLine($"{item.Key} ×{item.Value}");
        Console.WriteLine("\nUse Potions in battle. Find more items while exploring.");
        ConsoleUi.Pause();
    }

    private static void HealParty(Player player)
    {
        ConsoleUi.Clear();
        foreach (var pokemon in player.Party) pokemon.Heal();
        Console.WriteLine("Your party is fully healed!");
        AudioSystem.Play(SoundEffect.Heal);
        ConsoleUi.Pause();
    }

    private static void ToggleSound(Player? player = null)
    {
        AudioSystem.Enabled = !AudioSystem.Enabled;
        if (player is not null) player.SoundEnabled = AudioSystem.Enabled;
        Console.WriteLine($"Sound {(AudioSystem.Enabled ? "ON" : "OFF")}.");
        ConsoleUi.Pause();
    }

    private bool SaveGame(Player player)
    {
        var result = SaveSystem.Save(player);
        Console.WriteLine(result.Message);
        if (result.Success)
        {
            _savedSnapshot = SaveSystem.Snapshot(player);
            AudioSystem.Play(SoundEffect.Save);
        }
        ConsoleUi.Pause();
        return result.Success;
    }

    private bool ConfirmReturnToTitle(Player player)
    {
        if (_savedSnapshot == SaveSystem.Snapshot(player)) return true;
        Console.WriteLine("You have unsaved progress.");
        Console.WriteLine("1. Save and return  2. Discard and return  0. Keep playing");
        return ConsoleUi.ReadChoice(0, 2) switch
        {
            1 => SaveGame(player),
            2 => true,
            _ => false
        };
    }
}
