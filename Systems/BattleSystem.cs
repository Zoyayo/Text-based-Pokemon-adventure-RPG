using PokemonAdventure.Data;
using PokemonAdventure.Game;
using PokemonAdventure.Models;

namespace PokemonAdventure.Systems;

public sealed class BattleSystem(Player player, Random random)
{
    private enum BattleResult { Victory, Defeat, Caught, Escaped }
    private enum BagAction { NoTurnUsed, TurnUsed, Caught }
    private const int MaxAttackDrops = 4;

    private sealed class Combatant(Pokemon pokemon)
    {
        public Pokemon Pokemon { get; } = pokemon;
        public int AttackDrops { get; set; }
        public double EffectiveAttack => Pokemon.Attack * 4.0 / (4 + AttackDrops);
    }

    public void StartWildBattle(Pokemon wild, int experienceBonus = 0)
    {
        if (!player.HasUsablePokemon()) return;
        try
        {
            AnnounceOpponent(wild, $"A wild {wild.Name} Lv. {wild.Level} appeared!", true);
            var result = BattleOpponent(wild, true, out var active);
            if (result == BattleResult.Victory)
            {
                AudioSystem.StopBattleMusic();
                Console.WriteLine($"Wild {wild.Name} fainted!");
                AudioSystem.Play(SoundEffect.Victory);
                ConsoleUi.Pause();
                AwardExperience(wild, active, experienceBonus);
            }
            else if (result == BattleResult.Defeat) ShowDefeat();
        }
        finally { AudioSystem.StopBattleMusic(); }
    }

    public bool StartTrainerBattle(string trainerName, List<Pokemon> team)
    {
        if (!player.HasUsablePokemon() || team.Count == 0) return false;
        try
        {
            foreach (var enemy in team)
            {
                AnnounceOpponent(enemy, $"{trainerName} sent out {enemy.Name} Lv. {enemy.Level}!", false);
                if (BattleOpponent(enemy, false, out var active) != BattleResult.Victory)
                {
                    ShowDefeat();
                    return false;
                }
                Console.WriteLine($"{enemy.Name} fainted!");
                ConsoleUi.Pause();
                AwardExperience(enemy, active);
            }
            AudioSystem.StopBattleMusic();
            Console.WriteLine($"You defeated {trainerName}!");
            AudioSystem.Play(SoundEffect.Victory);
            ConsoleUi.Pause();
            return true;
        }
        finally { AudioSystem.StopBattleMusic(); }
    }

    private void AnnounceOpponent(Pokemon enemy, string message, bool wild)
    {
        ConsoleUi.Clear();
        player.SeenSpecies.Add(enemy.SpeciesId);
        Console.WriteLine(message);
        AudioSystem.Play(wild ? SoundEffect.Encounter : SoundEffect.SendOut);
        ConsoleUi.Pause();
        AudioSystem.PlayBattleMusic();
        SendOut(player.FirstAvailablePokemon()!);
    }

    private BattleResult BattleOpponent(Pokemon enemy, bool wild, out Pokemon active)
    {
        active = player.FirstAvailablePokemon()!;
        var ally = new Combatant(active);
        var opponent = new Combatant(enemy);
        while (!enemy.IsFainted && player.HasUsablePokemon())
        {
            ConsoleUi.Clear();
            Console.WriteLine($"{(wild ? "Wild " : "")}{enemy.Name} Lv. {enemy.Level} HP {enemy.CurrentHp}/{enemy.MaxHp}");
            Console.WriteLine($"{active.Name} Lv. {active.Level} HP {active.CurrentHp}/{active.MaxHp}");
            Console.WriteLine(wild ? "1. Fight  2. Bag  3. Pokémon  4. Run" : "1. Fight  2. Bag  3. Pokémon");
            switch (ConsoleUi.ReadChoice(1, wild ? 4 : 3, menuSound: false))
            {
                case 1:
                    Fight(ally, opponent);
                    break;
                case 2:
                    var bagAction = UseBag(active, enemy, wild);
                    if (bagAction == BagAction.Caught) return BattleResult.Caught;
                    if (bagAction == BagAction.NoTurnUsed) continue;
                    EnemyTurn(ally, opponent);
                    break;
                case 3:
                    var chosen = SwitchPokemon(active);
                    if (ReferenceEquals(chosen, active)) continue;
                    active = chosen;
                    ally = new Combatant(active);
                    EnemyTurn(ally, opponent);
                    break;
                case 4:
                    if (random.Next(100) < 70)
                    {
                        Console.WriteLine("You got away safely!");
                        AudioSystem.StopBattleMusic();
                        AudioSystem.Play(SoundEffect.RunAway);
                        ConsoleUi.Pause();
                        return BattleResult.Escaped;
                    }
                    Console.WriteLine("Couldn't escape!");
                    ConsoleUi.Pause();
                    EnemyTurn(ally, opponent);
                    break;
            }
            if (active.IsFainted)
            {
                Console.WriteLine($"{active.Name} fainted!");
                ConsoleUi.Pause();
                if (player.HasUsablePokemon())
                {
                    active = player.FirstAvailablePokemon()!;
                    ally = new Combatant(active);
                    SendOut(active);
                }
            }
        }
        return enemy.IsFainted ? BattleResult.Victory : BattleResult.Defeat;
    }

    public static bool PlayerActsFirst(Pokemon active, Pokemon enemy, Random random,
        int activePriority = 0, int enemyPriority = 0)
    {
        if (activePriority != enemyPriority) return activePriority > enemyPriority;
        return active.Speed > enemy.Speed || (active.Speed == enemy.Speed && random.Next(2) == 0);
    }

    private void Fight(Combatant ally, Combatant opponent)
    {
        var active = ally.Pokemon;
        var enemy = opponent.Pokemon;
        Console.WriteLine($"Choose a move for {active.Name}:");
        for (var i = 0; i < active.MoveNames.Count; i++)
        {
            var move = GameData.GetMove(active.MoveNames[i]);
            var description = move.Effect == MoveEffect.LowerAttack ? "Lowers Attack" : $"Power {move.Power}";
            var priority = move.Priority > 0 ? ", Priority" : "";
            Console.WriteLine($"{i + 1}. {move.Name} ({move.Type}, {description}{priority})");
        }
        var playerMove = GameData.GetMove(active.MoveNames[ConsoleUi.ReadChoice(1, active.MoveNames.Count, false) - 1]);
        var enemyMove = GameData.GetMove(enemy.MoveNames[random.Next(enemy.MoveNames.Count)]);
        if (PlayerActsFirst(active, enemy, random, playerMove.Priority, enemyMove.Priority))
        {
            Attack(ally, opponent, playerMove);
            if (!enemy.IsFainted) Attack(opponent, ally, enemyMove);
        }
        else
        {
            Attack(opponent, ally, enemyMove);
            if (!active.IsFainted) Attack(ally, opponent, playerMove);
        }
    }

    private BagAction UseBag(Pokemon active, Pokemon enemy, bool wild)
    {
        var balls = player.Bag.GetValueOrDefault(Player.PokeBall);
        var potions = player.Bag.GetValueOrDefault(Player.Potion);
        Console.WriteLine($"1. Poké Ball ×{balls}{(wild ? "" : " (not allowed in trainer battles)")}\n2. Potion ×{potions}\n3. Cancel");
        var choice = ConsoleUi.ReadChoice(1, 3, menuSound: false);
        if (choice == 3) return BagAction.NoTurnUsed;

        var action = choice == 1 ? ThrowPokeBall(enemy, wild, balls) : UsePotion(active, potions);
        ConsoleUi.Pause();
        return action;
    }

    private BagAction ThrowPokeBall(Pokemon enemy, bool wild, int balls)
    {
        if (!wild)
        {
            Console.WriteLine("You cannot catch another trainer's Pokémon.");
            return BagAction.NoTurnUsed;
        }
        if (balls <= 0)
        {
            Console.WriteLine("You have no Poké Balls.");
            return BagAction.NoTurnUsed;
        }

        player.Bag[Player.PokeBall]--;
        Console.WriteLine("You threw a Poké Ball...");
        var healthBonus = 1.0 - (double)enemy.CurrentHp / enemy.MaxHp;
        var chance = Math.Clamp(enemy.Species.CatchRate / 510.0 + healthBonus * 0.55, 0.15, 0.9);
        if (random.NextDouble() <= chance)
        {
            Console.WriteLine($"Gotcha! {enemy.Name} was caught!");
            AddCaughtPokemon(enemy);
            AudioSystem.StopBattleMusic();
            AudioSystem.Play(SoundEffect.Caught);
            return BagAction.Caught;
        }
        Console.WriteLine("Oh no! The Pokémon broke free!");
        return BagAction.TurnUsed;
    }

    private BagAction UsePotion(Pokemon active, int potions)
    {
        if (potions <= 0)
        {
            Console.WriteLine("You have no Potions.");
            return BagAction.NoTurnUsed;
        }
        if (active.CurrentHp == active.MaxHp)
        {
            Console.WriteLine("HP is already full.");
            return BagAction.NoTurnUsed;
        }

        player.Bag[Player.Potion]--;
        var healed = Math.Min(20, active.MaxHp - active.CurrentHp);
        active.CurrentHp += healed;
        Console.WriteLine($"{active.Name} recovered {healed} HP!");
        AudioSystem.Play(SoundEffect.Heal);
        return BagAction.TurnUsed;
    }

    private void AddCaughtPokemon(Pokemon pokemon)
    {
        player.RegisterCaughtSpecies(pokemon.SpeciesId);
        pokemon.Heal();
        if (player.Party.Count < Player.MaxPartySize)
        {
            player.Party.Add(pokemon);
            Console.WriteLine($"{pokemon.Name} joined your party!");
        }
        else
        {
            player.Storage.Add(pokemon);
            Console.WriteLine("Party full. Pokémon sent to Storage.");
        }
    }

    private Pokemon SwitchPokemon(Pokemon current)
    {
        Console.WriteLine("Choose Pokémon (0 to cancel):");
        for (var i = 0; i < player.Party.Count; i++)
        {
            var pokemon = player.Party[i];
            Console.WriteLine($"{i + 1}. {pokemon.Name} Lv. {pokemon.Level} HP {pokemon.CurrentHp}/{pokemon.MaxHp}");
        }
        var choice = ConsoleUi.ReadChoice(0, player.Party.Count, false);
        if (choice == 0 || player.Party[choice - 1].IsFainted || ReferenceEquals(player.Party[choice - 1], current))
        {
            Console.WriteLine("Switch cancelled. No turn used.");
            ConsoleUi.Pause();
            return current;
        }
        var chosen = player.Party[choice - 1];
        SendOut(chosen);
        return chosen;
    }

    private static void SendOut(Pokemon pokemon)
    {
        Console.WriteLine($"Go, {pokemon.Name}!");
        AudioSystem.Play(SoundEffect.SendOut);
        ConsoleUi.Pause();
    }

    private void EnemyTurn(Combatant ally, Combatant opponent)
    {
        var moves = opponent.Pokemon.MoveNames;
        Attack(opponent, ally, GameData.GetMove(moves[random.Next(moves.Count)]));
    }

    private void Attack(Combatant source, Combatant target, Move move)
    {
        var attacker = source.Pokemon;
        var defender = target.Pokemon;
        ConsoleUi.Clear();
        Console.WriteLine($"{attacker.Name} used {move.Name}!");
        AudioSystem.Play(SoundEffect.Attack);
        if (random.Next(100) >= move.Accuracy)
        {
            Console.WriteLine("But it missed!");
            ConsoleUi.Pause();
            return;
        }
        var effectiveness = TypeEffectiveness(move.Type, defender.Species.Type);
        if (effectiveness == 0)
        {
            Console.WriteLine("It has no effect!");
            ConsoleUi.Pause();
            return;
        }
        if (move.Effect == MoveEffect.LowerAttack)
        {
            if (target.AttackDrops == MaxAttackDrops)
                Console.WriteLine($"{defender.Name}'s Attack cannot go any lower.");
            else
            {
                target.AttackDrops++;
                Console.WriteLine($"{defender.Name}'s Attack fell!");
            }
            ConsoleUi.Pause();
            return;
        }
        var critical = random.Next(100) < 8;
        var baseDamage = ((2.0 * attacker.Level / 5 + 2) * move.Power * source.EffectiveAttack / Math.Max(1, defender.Defense)) / 12 + 2;
        var criticalMultiplier = critical ? 1.5 : 1;
        var damageVariation = 0.85 + random.NextDouble() * 0.15;
        var adjustedDamage = (int)(baseDamage * effectiveness * criticalMultiplier * damageVariation);
        var damage = Math.Min(defender.CurrentHp, Math.Max(1, adjustedDamage));
        defender.CurrentHp -= damage;
        if (effectiveness > 1) Console.WriteLine("It's super effective!");
        if (effectiveness < 1) Console.WriteLine("It's not very effective...");
        if (critical) Console.WriteLine("A critical hit!");
        Console.WriteLine($"{defender.Name} lost {damage} HP. HP: {defender.CurrentHp}/{defender.MaxHp}");
        ConsoleUi.Pause();
    }

    private void AwardExperience(Pokemon enemy, Pokemon active, int bonus = 0)
    {
        active.ReceiveExperience(15 + enemy.Level * 8 + bonus,
            message =>
            {
                Console.WriteLine(message);
                ConsoleUi.Pause();
            },
            progress =>
            {
                AudioSystem.Play(progress == PokemonProgress.LevelUp ? SoundEffect.LevelUp : SoundEffect.Evolution);
                player.RegisterCaughtSpecies(active.SpeciesId);
            });
    }

    private static void ShowDefeat()
    {
        AudioSystem.StopBattleMusic();
        Console.WriteLine("All your Pokémon fainted. Visit the Pokémon Center!");
        ConsoleUi.Pause();
    }

    public static double TypeEffectiveness(PokemonType attack, PokemonType defend) => (attack, defend) switch
    {
        (PokemonType.Electric, PokemonType.Ground) or (PokemonType.Ground, PokemonType.Flying) => 0,
        (PokemonType.Grass, PokemonType.Water or PokemonType.Rock or PokemonType.Ground) => 2,
        (PokemonType.Fire, PokemonType.Grass or PokemonType.Bug) => 2,
        (PokemonType.Water, PokemonType.Fire or PokemonType.Rock or PokemonType.Ground) => 2,
        (PokemonType.Flying, PokemonType.Grass or PokemonType.Bug) => 2,
        (PokemonType.Electric, PokemonType.Water or PokemonType.Flying) => 2,
        (PokemonType.Bug, PokemonType.Grass) => 2,
        (PokemonType.Rock, PokemonType.Fire or PokemonType.Flying or PokemonType.Bug) => 2,
        (PokemonType.Ground, PokemonType.Fire or PokemonType.Electric or PokemonType.Rock) => 2,
        (PokemonType.Normal, PokemonType.Rock) => 0.5,
        (PokemonType.Grass, PokemonType.Fire or PokemonType.Grass or PokemonType.Flying or PokemonType.Bug) => 0.5,
        (PokemonType.Fire, PokemonType.Fire or PokemonType.Water or PokemonType.Rock) => 0.5,
        (PokemonType.Water, PokemonType.Water or PokemonType.Grass) => 0.5,
        (PokemonType.Flying, PokemonType.Electric or PokemonType.Rock) => 0.5,
        (PokemonType.Electric, PokemonType.Electric or PokemonType.Grass) => 0.5,
        (PokemonType.Bug, PokemonType.Fire or PokemonType.Flying) => 0.5,
        (PokemonType.Rock, PokemonType.Ground) => 0.5,
        (PokemonType.Ground, PokemonType.Grass or PokemonType.Bug) => 0.5,
        _ => 1
    };
}
