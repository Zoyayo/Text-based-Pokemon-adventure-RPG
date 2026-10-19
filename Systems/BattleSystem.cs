using PokemonAdventure.Data;
using PokemonAdventure.Models;

namespace PokemonAdventure.Systems;

public sealed class BattleSystem
{
    private readonly Player _player;
    private readonly Random _random;

    public BattleSystem(Player player, Random random)
    {
        _player = player;
        _random = random;
    }

    public void StartWildBattle(Pokemon wild)
    {
        _player.SeenSpecies.Add(wild.SpeciesId);
        var active = _player.FirstAvailablePokemon()!;
        Console.WriteLine($"\nA wild {wild.Name} Lv. {wild.Level} appeared!");
        Pause();

        while (!wild.IsFainted && _player.HasUsablePokemon())
        {
            ShowStatus(active, wild);
            Console.WriteLine("1. Fight  2. Bag  3. Pokémon  4. Run");
            switch (ReadChoice(1, 4))
            {
                case 1:
                    Fight(active, wild);
                    break;
                case 2:
                    var usedTurn = UseBag(active, wild, out var caught);
                    if (caught) return;
                    if (!usedTurn) continue;
                    if (!wild.IsFainted) EnemyTurn(active, wild);
                    break;
                case 3:
                    active = SwitchPokemon(active);
                    if (!wild.IsFainted) EnemyTurn(active, wild);
                    break;
                case 4:
                    if (_random.Next(100) < 70)
                    {
                        Console.WriteLine("You got away safely!");
                        Pause();
                        return;
                    }
                    Console.WriteLine("Couldn't escape!");
                    EnemyTurn(active, wild);
                    break;
            }

            if (active.IsFainted && _player.HasUsablePokemon())
            {
                Console.WriteLine($"{active.Name} fainted!");
                active = _player.FirstAvailablePokemon()!;
                Console.WriteLine($"Go, {active.Name}!");
            }
        }

        if (wild.IsFainted)
        {
            Console.WriteLine($"Wild {wild.Name} fainted!");
            active.ReceiveExperience(15 + wild.Level * 8, Console.WriteLine);
        }
        else Console.WriteLine("All of your Pokémon fainted. Return to the Pokémon Center!");
        Pause();
    }

    private void Fight(Pokemon active, Pokemon wild)
    {
        Console.WriteLine($"\nChoose a move for {active.Name}:");
        for (var i = 0; i < active.MoveNames.Count; i++)
        {
            var move = GameData.GetMove(active.MoveNames[i]);
            Console.WriteLine($"{i + 1}. {move.Name} ({move.Type}, Power {move.Power})");
        }

        var playerMove = GameData.GetMove(active.MoveNames[ReadChoice(1, active.MoveNames.Count) - 1]);
        var enemyMove = GameData.GetMove(wild.MoveNames[_random.Next(wild.MoveNames.Count)]);
        if (active.Speed >= wild.Speed || _random.Next(100) < 50)
        {
            Attack(active, wild, playerMove);
            if (!wild.IsFainted) Attack(wild, active, enemyMove);
        }
        else
        {
            Attack(wild, active, enemyMove);
            if (!active.IsFainted) Attack(active, wild, playerMove);
        }
    }

    private bool UseBag(Pokemon active, Pokemon wild, out bool caught)
    {
        caught = false;
        var balls = _player.Bag.GetValueOrDefault("Poké Ball");
        var potions = _player.Bag.GetValueOrDefault("Potion");
        Console.WriteLine($"\n1. Poké Ball ×{balls}\n2. Potion ×{potions}\n3. Cancel");
        switch (ReadChoice(1, 3))
        {
            case 1 when balls > 0:
                _player.Bag["Poké Ball"]--;
                Console.WriteLine("You threw a Poké Ball...");
                var healthBonus = 1.0 - (double)wild.CurrentHp / wild.MaxHp;
                var chance = Math.Clamp(wild.Species.CatchRate / 510.0 + healthBonus * 0.55, 0.15, 0.9);
                if (_random.NextDouble() <= chance)
                {
                    Console.WriteLine($"Gotcha! {wild.Name} was caught!");
                    AddCaughtPokemon(wild);
                    Pause();
                    caught = true;
                    return true;
                }
                Console.WriteLine("Oh no! The Pokémon broke free!");
                Pause();
                return true;
            case 1:
                Console.WriteLine("You have no Poké Balls.");
                Pause();
                return false;
            case 2 when potions > 0:
                _player.Bag["Potion"]--;
                var healed = Math.Min(20, active.MaxHp - active.CurrentHp);
                active.CurrentHp += healed;
                Console.WriteLine($"{active.Name} recovered {healed} HP!");
                Pause();
                return true;
            case 2:
                Console.WriteLine("You have no Potions.");
                Pause();
                return false;
            default:
                return false;
        }
    }

    private void AddCaughtPokemon(Pokemon pokemon)
    {
        _player.CaughtSpecies.Add(pokemon.SpeciesId);
        pokemon.Heal();
        if (_player.Party.Count < 6)
        {
            _player.Party.Add(pokemon);
            Console.WriteLine($"{pokemon.Name} joined your party!");
        }
        else
        {
            _player.Storage.Add(pokemon);
            Console.WriteLine("Your party is full. The Pokémon was sent to Storage.");
        }
    }

    private Pokemon SwitchPokemon(Pokemon current)
    {
        Console.WriteLine("\nChoose Pokémon (0 to cancel):");
        for (var i = 0; i < _player.Party.Count; i++)
        {
            var pokemon = _player.Party[i];
            Console.WriteLine($"{i + 1}. {pokemon.Name} Lv. {pokemon.Level} HP {pokemon.CurrentHp}/{pokemon.MaxHp}{(pokemon.IsFainted ? " (Fainted)" : "")}");
        }
        var selection = ReadChoice(0, _player.Party.Count);
        if (selection == 0 || _player.Party[selection - 1].IsFainted || _player.Party[selection - 1] == current)
        {
            Console.WriteLine("Switch cancelled.");
            return current;
        }
        var chosen = _player.Party[selection - 1];
        Console.WriteLine($"Go, {chosen.Name}!");
        return chosen;
    }

    private void EnemyTurn(Pokemon active, Pokemon wild) => Attack(wild, active, GameData.GetMove(wild.MoveNames[_random.Next(wild.MoveNames.Count)]));

    private void Attack(Pokemon attacker, Pokemon defender, Move move)
    {
        Console.WriteLine($"{attacker.Name} used {move.Name}!");
        if (_random.Next(100) >= move.Accuracy) { Console.WriteLine("But it missed!"); return; }
        var effectiveness = TypeEffectiveness(move.Type, defender.Species.Type);
        var critical = _random.Next(100) < 8;
        var baseDamage = ((2.0 * attacker.Level / 5 + 2) * move.Power * attacker.Attack / Math.Max(1, defender.Defense)) / 12 + 2;
        var damage = Math.Max(1, (int)(baseDamage * effectiveness * (critical ? 1.5 : 1) * (0.85 + _random.NextDouble() * 0.15)));
        defender.CurrentHp = Math.Max(0, defender.CurrentHp - damage);
        if (effectiveness > 1) Console.WriteLine("It's super effective!");
        if (effectiveness < 1) Console.WriteLine("It's not very effective...");
        if (critical) Console.WriteLine("A critical hit!");
        Console.WriteLine($"{defender.Name} lost {damage} HP.");
    }

    private static double TypeEffectiveness(PokemonType attack, PokemonType defend) => (attack, defend) switch
    {
        (PokemonType.Grass, PokemonType.Water) => 2,
        (PokemonType.Grass, PokemonType.Fire) or (PokemonType.Grass, PokemonType.Flying) => 0.5,
        (PokemonType.Fire, PokemonType.Grass) => 2,
        (PokemonType.Fire, PokemonType.Water) => 0.5,
        (PokemonType.Water, PokemonType.Fire) => 2,
        (PokemonType.Water, PokemonType.Grass) => 0.5,
        _ => 1
    };

    private static void ShowStatus(Pokemon active, Pokemon wild) => Console.WriteLine($"\nWild {wild.Name} Lv. {wild.Level}  HP: {wild.CurrentHp}/{wild.MaxHp}\n{active.Name} Lv. {active.Level}  HP: {active.CurrentHp}/{active.MaxHp}");
    private static int ReadChoice(int min, int max)
    {
        while (true)
        {
            if (int.TryParse(Console.ReadLine(), out var result) && result >= min && result <= max) return result;
            Console.Write($"Choose {min}-{max}: ");
        }
    }
    private static void Pause() { Console.WriteLine("\nPress Enter to continue..."); Console.ReadLine(); }
}
