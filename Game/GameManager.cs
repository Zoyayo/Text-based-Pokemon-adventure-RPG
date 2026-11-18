using PokemonAdventure.Data;
using PokemonAdventure.Models;
using PokemonAdventure.Systems;

namespace PokemonAdventure.Game;

public sealed class GameManager
{
    private readonly Random _random = new();
    private Player? _player;

    public void Run()
    {
        while (true)
        {
            ConsoleUi.Clear();
            Console.WriteLine("========================================");
            Console.WriteLine("   TEXT-BASED POKÉMON ADVENTURE RPG");
            Console.WriteLine("              BETA VERSION");
            Console.WriteLine("========================================");
            Console.WriteLine("1. New Game");
            Console.WriteLine($"2. Load Game{(SaveSystem.SaveExists() ? string.Empty : " (no save found)")}");
            Console.WriteLine("3. Exit");
            Console.WriteLine($"4. Sound: {(AudioSystem.Enabled ? "ON" : "OFF")}");
            Console.Write("Choose: ");
            switch (ConsoleUi.ReadChoice(1, 4))
            {
                case 1: NewGame(); MainLoop(); break;
                case 2:
                    _player = SaveSystem.Load();
                    if (_player is null)
                    {
                        Console.WriteLine("No valid save file was found.");
                        ConsoleUi.Pause();
                    }
                    else
                    {
                        Console.WriteLine($"Welcome back, {_player.Name}!");
                        ConsoleUi.Pause();
                        MainLoop();
                    }
                    break;
                case 3: return;
                case 4:
                    AudioSystem.Enabled = !AudioSystem.Enabled;
                    Console.WriteLine($"Sound {(AudioSystem.Enabled ? "ON" : "OFF")}.");
                    ConsoleUi.Pause();
                    break;
            }
        }
    }

    private void NewGame()
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
        _player = new Player { Name = name };
        Console.WriteLine($"\nNice to meet you, {name}. Choose your starter:");
        Console.WriteLine("1. Bulbasaur (Grass)");
        Console.WriteLine("2. Charmander (Fire)");
        Console.WriteLine("3. Squirtle (Water)");
        var starterId = ConsoleUi.ReadChoice(1, 3) switch { 1 => "bulbasaur", 2 => "charmander", _ => "squirtle" };
        var starter = Pokemon.Create(starterId, 5);
        _player.Party.Add(starter);
        _player.SeenSpecies.Add(starterId);
        _player.CaughtSpecies.Add(starterId);
        Console.WriteLine($"You received {starter.Name}! Route 1 is waiting for you.");
        AudioSystem.Play(SoundEffect.Caught);
        ConsoleUi.Pause();
    }

    private void MainLoop()
    {
        while (_player is not null)
        {
            ConsoleUi.Clear();
            Console.WriteLine("================================");
            Console.WriteLine("PALLET TOWN — BETA");
            Console.WriteLine($"Trainer: {_player.Name}  |  Party: {_player.Party.Count}/6");
            Console.WriteLine("================================");
            Console.WriteLine("1. Explore Route 1");
            Console.WriteLine("2. Party");
            Console.WriteLine("3. Pokédex");
            Console.WriteLine("4. Bag");
            Console.WriteLine("5. Pokémon Center");
            Console.WriteLine("6. Save Game");
            Console.WriteLine("7. Return to Title");
            Console.Write("Choose: ");
            switch (ConsoleUi.ReadChoice(1, 7))
            {
                case 1: Explore(); break;
                case 2: ShowParty(); break;
                case 3: ShowPokedex(); break;
                case 4: ShowBag(); break;
                case 5: HealParty(); break;
                case 6:
                    SaveSystem.Save(_player);
                    Console.WriteLine("Game saved to save.json.");
                    AudioSystem.Play(SoundEffect.Save);
                    ConsoleUi.Pause();
                    break;
                case 7:
                    AudioSystem.StopAll();
                    _player = null;
                    break;
            }
        }
    }

    private void Explore()
    {
        ConsoleUi.Clear();
        Console.WriteLine("You are exploring Route 1...");
        if (!_player!.HasUsablePokemon())
        {
            Console.WriteLine("Your party needs healing. Visit the Pokémon Center first.");
            ConsoleUi.Pause();
            return;
        }
        if (_random.Next(100) < 70) new BattleSystem(_player, _random).StartWildBattle(GameData.CreateRouteOneEncounter(_random));
        else { Console.WriteLine("You found nothing this time."); ConsoleUi.Pause(); }
    }

    private void ShowParty()
    {
        ConsoleUi.Clear();
        Console.WriteLine("YOUR PARTY");
        for (var i = 0; i < _player!.Party.Count; i++)
        {
            var pokemon = _player.Party[i];
            Console.WriteLine($"{i + 1}. {pokemon.Name} Lv. {pokemon.Level} [{pokemon.Species.Type}]");
            Console.WriteLine($"   HP {pokemon.CurrentHp}/{pokemon.MaxHp} | EXP {pokemon.Experience}/{pokemon.ExperienceToLevel}");
            Console.WriteLine($"   Moves: {string.Join(", ", pokemon.MoveNames)}");
        }
        Console.WriteLine($"\nStorage: {_player.Storage.Count} Pokémon");
        ConsoleUi.Pause();
    }

    private void ShowPokedex()
    {
        ConsoleUi.Clear();
        Console.WriteLine($"POKÉDEX — Seen: {_player!.SeenSpecies.Count}/{GameData.AllSpecies.Count}, Caught: {_player.CaughtSpecies.Count}/{GameData.AllSpecies.Count}");
        foreach (var species in GameData.AllSpecies.OrderBy(s => s.Name))
        {
            var marker = _player.CaughtSpecies.Contains(species.Id) ? "Caught" : _player.SeenSpecies.Contains(species.Id) ? "Seen" : "-----";
            Console.WriteLine($"[{marker}] {species.Name}");
        }
        ConsoleUi.Pause();
    }

    private void ShowBag()
    {
        ConsoleUi.Clear();
        Console.WriteLine("BAG");
        foreach (var item in _player!.Bag) Console.WriteLine($"{item.Key} ×{item.Value}");
        Console.WriteLine("\nPotions can be used during battle in this beta.");
        ConsoleUi.Pause();
    }

    private void HealParty()
    {
        foreach (var pokemon in _player!.Party) pokemon.Heal();
        Console.WriteLine("Your Pokémon are fully healed!");
        AudioSystem.Play(SoundEffect.Heal);
        ConsoleUi.Pause();
    }
}
