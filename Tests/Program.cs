using System.Text.Json;
using PokemonAdventure.Data;
using PokemonAdventure.Game;
using PokemonAdventure.Models;
using PokemonAdventure.Systems;

var failures = 0;
var checks = 0;
var originalDirectory = Environment.CurrentDirectory;
var originalAudio = Environment.GetEnvironmentVariable("POKEMON_AUDIO");
var testDirectory = Path.Combine(Path.GetTempPath(), "pokemon-beta11-checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(testDirectory);
Environment.SetEnvironmentVariable("POKEMON_AUDIO", "0");
try
{
    Check("20 species", GameData.AllSpecies.Count == 20);
    foreach (var species in GameData.AllSpecies)
    {
        var pokemon = Pokemon.Create(species.Id, 1);
        Check($"Playable species: {species.Id}", pokemon.CurrentHp == pokemon.MaxHp && pokemon.MoveNames.Count > 0 &&
            pokemon.MoveNames.All(GameData.HasMove));
    }
    foreach (var (starter, middle, final, finalLevel) in new[]
    {
        ("bulbasaur", "ivysaur", "venusaur", 32),
        ("charmander", "charmeleon", "charizard", 36),
        ("squirtle", "wartortle", "blastoise", 36),
        ("caterpie", "metapod", "butterfree", 10),
        ("weedle", "kakuna", "beedrill", 10)
    })
    {
        var level = starter is "caterpie" or "weedle" ? 6 : 15;
        var pokemon = Pokemon.Create(starter, level);
        var events = new List<PokemonProgress>();
        pokemon.ReceiveExperience(pokemon.ExperienceToLevel, _ => { }, events.Add);
        Check($"{starter} first evolution", pokemon.SpeciesId == middle && events.Contains(PokemonProgress.Evolution));
        while (pokemon.Level < finalLevel) pokemon.ReceiveExperience(pokemon.ExperienceToLevel, _ => { });
        Check($"{starter} final evolution", pokemon.SpeciesId == final && pokemon.MoveNames.Count <= 4 && pokemon.CurrentHp <= pokemon.MaxHp);
    }
    var capped = Pokemon.Create("pikachu", 99);
    capped.ReceiveExperience(10000, _ => { });
    Check("Level cap and EXP", capped.Level == 100 && capped.Experience == 0);
    var legacyHighLevel = Pokemon.Create("bulbasaur", 40);
    legacyHighLevel.ReceiveExperience(legacyHighLevel.ExperienceToLevel, _ => { });
    Check("High-level old starter catches up evolution", legacyHighLevel.SpeciesId == "venusaur");

    var player = NewPlayer();
    var original = player.Party[0];
    original.CurrentHp = 9;
    original.Experience = 13;
    var moves = original.MoveNames.ToArray();
    Check("Cannot deposit last Pokémon", !PartySystem.Deposit(player, 0).Success);
    Check("Reject invalid storage index", !PartySystem.Withdraw(player, -1).Success);
    player.Party.Add(Pokemon.Create("pikachu", 7));
    Check("Deposit succeeds", PartySystem.Deposit(player, 0).Success && ReferenceEquals(player.Storage[0], original));
    Check("Withdraw succeeds", PartySystem.Withdraw(player, 0).Success && ReferenceEquals(player.Party[1], original));
    Check("HP EXP moves preserved", original.CurrentHp == 9 && original.Experience == 13 && moves.SequenceEqual(original.MoveNames));
    Check("Swap order", PartySystem.SwapOrder(player, 0, 1).Success && ReferenceEquals(player.Party[0], original));
    Check("Reject same slot", !PartySystem.SwapOrder(player, 0, 0).Success);
    while (player.Party.Count < 6) player.Party.Add(Pokemon.Create("rattata", 4));
    var stored = Pokemon.Create("caterpie", 6);
    player.Storage.Add(stored);
    Check("Cannot withdraw into full party", !PartySystem.Withdraw(player, 0).Success);
    Check("Exchange with full party", PartySystem.Exchange(player, 0, 0).Success &&
        ReferenceEquals(player.Party[0], stored) && ReferenceEquals(player.Storage[0], original) && player.Party.Count == 6);

    var explorer = NewPlayer();
    Check("Route 2 locked initially", !ExplorationSystem.CanExploreRoute(explorer, 2));
    Check("Brock reward once", ExplorationSystem.AwardBrockVictory(explorer) && explorer.Bag["Poké Ball"] == 13 && explorer.Bag["Potion"] == 5);
    Check("No duplicate Gym reward", !ExplorationSystem.AwardBrockVictory(explorer) && explorer.Bag["Poké Ball"] == 13);
    Check("Route 2 unlocked", ExplorationSystem.CanExploreRoute(explorer, 2));
    Check("Unknown route blocked", !ExplorationSystem.CanExploreRoute(explorer, 3));
    Check("Encounter boundary", ExplorationSystem.RollEvent(new FixedRandom(64)) == ExplorationEvent.WildPokemon);
    Check("Item boundary", ExplorationSystem.RollEvent(new FixedRandom(65)) == ExplorationEvent.Item);
    Check("Nothing boundary", ExplorationSystem.RollEvent(new FixedRandom(90)) == ExplorationEvent.Nothing);
    var found = ExplorationSystem.FindItem(explorer, new FixedRandom(0));
    Check("Found item added", found.Item == "Poké Ball" && found.Amount == 1 && explorer.Bag["Poké Ball"] == 14);
    explorer.Bag["Potion"] = int.MaxValue;
    Check("Item count cannot overflow", ExplorationSystem.AddItem(explorer, "Potion", 2) == 0);
    var encounters = Enumerable.Range(0, 100).Select(roll => GameData.CreateRouteTwoEncounter(new FixedRandom(roll))).ToArray();
    Check("Route 2 species and levels", encounters.Select(p => p.SpeciesId).Distinct().Count() == 4 && encounters.All(p => p.Level is >= 5 and <= 9));

    var fast = Pokemon.Create("pikachu", 20);
    var slow = Pokemon.Create("geodude", 20);
    Check("Faster always first", BattleSystem.PlayerActsFirst(fast, slow, new FixedRandom(1)));
    Check("Slower never first", !BattleSystem.PlayerActsFirst(slow, fast, new FixedRandom(0)));
    Check("Speed tie randomized", BattleSystem.PlayerActsFirst(fast, fast, new FixedRandom(0)) && !BattleSystem.PlayerActsFirst(fast, fast, new FixedRandom(1)));
    var priority = GameData.GetMove("Quick Attack").Priority;
    Check("Quick Attack beats higher Speed", priority > 0 &&
        BattleSystem.PlayerActsFirst(slow, fast, new FixedRandom(1), priority, 0));
    Check("Speed decides equal priority", !BattleSystem.PlayerActsFirst(slow, fast, new FixedRandom(0), priority, priority));
    Check("Electric immunity", BattleSystem.TypeEffectiveness(PokemonType.Electric, PokemonType.Ground) == 0);
    Check("Ground immunity", BattleSystem.TypeEffectiveness(PokemonType.Ground, PokemonType.Flying) == 0);
    Check("Water beats Rock", BattleSystem.TypeEffectiveness(PokemonType.Water, PokemonType.Rock) == 2);
    Check("Rock resists Normal", BattleSystem.TypeEffectiveness(PokemonType.Normal, PokemonType.Rock) == 0.5);

    var savePath = Path.Combine(testDirectory, "roundtrip.json");
    player.SoundEnabled = false;
    player.Badges.Add(GameData.BoulderBadge);
    Check("Save succeeds", SaveSystem.Save(player, savePath).Success);
    var loaded = SaveSystem.Load(savePath).Player!;
    Check("Save roundtrip", loaded.Party.Count == 6 && loaded.Storage[0].CurrentHp == 9 &&
        loaded.Storage[0].Experience == 13 && !loaded.SoundEnabled && loaded.Badges.Contains(GameData.BoulderBadge));
    player.Name = "Second save";
    Check("Second save creates backup", SaveSystem.Save(player, savePath).Success && File.Exists(savePath + ".bak"));
    File.WriteAllText(savePath, "{broken");
    var recovered = SaveSystem.Load(savePath);
    Check("Corrupt save recovers backup", recovered.RecoveredBackup && recovered.Player?.Name == "Tester");
    var backupBefore = File.ReadAllText(savePath + ".bak");
    Check("Repair save preserves good backup", SaveSystem.Save(recovered.Player!, savePath).Success && File.ReadAllText(savePath + ".bak") == backupBefore);
    File.Delete(savePath);
    Check("Missing primary recovers backup", SaveSystem.Load(savePath).RecoveredBackup);
    Check("Missing file handled", SaveSystem.Load(Path.Combine(testDirectory, "absent.json")).Player is null);
    Check("Write error handled", !SaveSystem.Save(NewPlayer(), Path.Combine(testDirectory, "missing-directory", "save.json")).Success);

    var legacyPath = Path.Combine(testDirectory, "legacy.json");
    File.WriteAllText(legacyPath, """
    {"Name":"Beta 1.0","Party":[{"SpeciesId":"bulbasaur","Level":5,"Experience":12,"CurrentHp":18,"MoveNames":["Tackle","Growl"]}],"Storage":[],"Bag":{"Poké Ball":8,"Potion":3},"SeenSpecies":["bulbasaur"],"CaughtSpecies":["bulbasaur"]}
    """);
    var legacy = SaveSystem.Load(legacyPath).Player;
    Check("Beta 1.0 compatible", legacy is not null && legacy.SoundEnabled && legacy.Badges.Count == 0 &&
        legacy.Party[0].CurrentHp == 18 && legacy.Party[0].Experience == 12);
    foreach (var (name, mutate) in new (string, Action<Player>)[]
    {
        ("unknown species", p => p.Party[0].SpeciesId = "missing"),
        ("invalid level", p => p.Party[0].Level = 0),
        ("invalid HP", p => p.Party[0].CurrentHp = 99999),
        ("unknown move", p => p.Party[0].MoveNames = ["missing"]),
        ("empty party", p => p.Party.Clear()),
        ("null storage", p => p.Storage = null!),
        ("negative items", p => p.Bag["Potion"] = -1)
    })
    {
        var invalid = NewPlayer();
        mutate(invalid);
        var invalidPath = Path.Combine(testDirectory, "invalid.json");
        File.WriteAllText(invalidPath, JsonSerializer.Serialize(invalid));
        Check($"Reject corrupt data: {name}", SaveSystem.Load(invalidPath).Player is null);
    }

    var cancelPlayer = NewPlayer();
    var cancelHp = cancelPlayer.Party[0].CurrentHp;
    var cancelled = RunUi(() => new BattleSystem(cancelPlayer, new FixedRandom(0)).StartWildBattle(Pokemon.Create("rattata", 3)),
        "", "", "3", "0", "", "4", "");
    Check("Cancel switch does not consume turn", cancelPlayer.Party[0].CurrentHp == cancelHp && !cancelled.Contains("used Tackle"));

    var growlPlayer = NewPlayer();
    var growlUser = growlPlayer.Party[0];
    var growlTarget = Pokemon.Create("geodude", 3);
    var growlOutput = RunUi(() => new BattleSystem(growlPlayer, new FixedRandom(0)).StartWildBattle(growlTarget),
        "", "", "1", "2", "", "", "4", "");
    var hpAfterGrowl = growlUser.CurrentHp;
    Check("Growl lowers Attack without dealing damage", growlOutput.Contains("Attack fell!") &&
        growlTarget.CurrentHp == growlTarget.MaxHp);
    growlUser.Heal();
    RunUi(() => new BattleSystem(growlPlayer, new FixedRandom(0)).StartWildBattle(growlTarget),
        "", "", "1", "1", "", "", "4", "");
    Check("Growl reduces damage and wears off after battle", hpAfterGrowl > growlUser.CurrentHp &&
        growlTarget.Attack == growlTarget.Species.BaseAttack + growlTarget.Level * 2);

    var quickPlayer = NewPlayer();
    quickPlayer.Party[0] = Pokemon.Create("rattata", 4);
    var quickEnemy = Pokemon.Create("pikachu", 6);
    quickEnemy.MoveNames = ["Tackle"];
    var quickOutput = RunUi(() => new BattleSystem(quickPlayer, new FixedRandom(0)).StartWildBattle(quickEnemy),
        "", "", "1", "2", "", "", "4", "");
    var fastMoveIndex = quickOutput.IndexOf("Rattata used Quick Attack!", StringComparison.Ordinal);
    var ordinaryMoveIndex = quickOutput.IndexOf("Pikachu used Tackle!", StringComparison.Ordinal);
    Check("Quick Attack goes first in an actual fight", fastMoveIndex >= 0 && ordinaryMoveIndex > fastMoveIndex &&
        quickPlayer.Party[0].Speed < quickEnemy.Speed);

    var trainingPlayer = NewPlayer();
    trainingPlayer.Party[0] = Pokemon.Create("charmander", 5);
    var trainingStarter = trainingPlayer.Party[0];
    trainingStarter.Experience = trainingStarter.ExperienceToLevel - 35;
    var trainingWild = Pokemon.Create("rattata", 2);
    trainingWild.CurrentHp = 1;
    RunUi(() => new BattleSystem(trainingPlayer, new FixedRandom(0)).StartWildBattle(trainingWild, GameData.RouteOneExperienceBonus),
        "", "", "1", "1", "", "", "", "", "");
    Check("Route 1 bonus can unlock Charmander's coverage at level 6", trainingStarter.Level == 6 &&
        trainingStarter.MoveNames.Contains("Mud Slap"));
    var potionPlayer = NewPlayer();
    RunUi(() => new BattleSystem(potionPlayer, new FixedRandom(0)).StartWildBattle(Pokemon.Create("rattata", 3)),
        "", "", "2", "2", "", "4", "");
    Check("Full HP potion not consumed", potionPlayer.Bag["Potion"] == 3 && potionPlayer.Party[0].CurrentHp == potionPlayer.Party[0].MaxHp);

    var capturePlayer = NewPlayer();
    while (capturePlayer.Party.Count < 6) capturePlayer.Party.Add(Pokemon.Create("pidgey", 3));
    var captured = Pokemon.Create("caterpie", 5);
    RunUi(() => new BattleSystem(capturePlayer, new FixedRandom(0)).StartWildBattle(captured), "", "", "2", "1", "");
    Check("Capture overflow goes to storage", capturePlayer.Storage.Count == 1 && ReferenceEquals(capturePlayer.Storage[0], captured) &&
        capturePlayer.Bag["Poké Ball"] == 7 && capturePlayer.CaughtSpecies.Contains("caterpie"));

    var switched = NewPlayer();
    switched.Party.Add(Pokemon.Create("squirtle", 100));
    RunUi(() => new BattleSystem(switched, new FixedRandom(0)).StartWildBattle(Pokemon.Create("rattata", 1)),
        "", "", "3", "2", "", "", "1", "1", "", "", "");
    Check("EXP belongs to active battler after switching", switched.Party[0].Experience == 0);

    var champion = NewPlayer();
    champion.Party[0] = Pokemon.Create("squirtle", 100);
    var trainerWon = false;
    RunUi(() => trainerWon = new BattleSystem(champion, new FixedRandom(0)).StartTrainerBattle("Brock", GameData.CreateBrockTeam()),
        "", "", "1", "1", "", "", "", "", "1", "1", "", "", "");
    Check("Complete Brock battle", trainerWon && champion.SeenSpecies.Contains("onix") && champion.SeenSpecies.Contains("geodude"));

    var trainerBag = NewPlayer();
    trainerBag.Party[0] = Pokemon.Create("squirtle", 100);
    RunUi(() => new BattleSystem(trainerBag, new FixedRandom(0)).StartTrainerBattle("Test trainer", [Pokemon.Create("rattata", 1)]),
        "", "", "2", "1", "", "1", "1", "", "", "");
    Check("Cannot capture trainer Pokémon", trainerBag.Bag["Poké Ball"] == 8 && trainerBag.Party.Count == 1);

    var loser = NewPlayer();
    loser.Party[0] = Pokemon.Create("bulbasaur", 1);
    loser.Party[0].CurrentHp = 1;
    var defeated = RunUi(() => new BattleSystem(loser, new FixedRandom(0)).StartWildBattle(Pokemon.Create("rattata", 100)),
        "", "", "1", "1", "", "", "");
    Check("Defeat returns safely", loser.Party[0].IsFainted && defeated.Contains("Visit the Pokémon Center"));

    Environment.CurrentDirectory = testDirectory;
    AudioSystem.Enabled = true;
    var menuOutput = RunUi(() => new GameManager().Run(),
        "1", "Beta Check", "1", "", "9", "0", "10", "", "7", "0", "6", "", "7", "2", "", "7", "3");
    Check("Menus save/load and unsaved cancel", menuOutput.Contains("BETA 1.1") && menuOutput.Contains("You have unsaved progress") &&
        menuOutput.Contains("Welcome back, Beta Check") && SaveSystem.Load().Player?.SoundEnabled == false);
    var diskSave = File.ReadAllText("save.json");
    RunUi(() => new GameManager().Run(), "1", "Discard me", "2", "", "7", "2", "3");
    Check("Discard does not overwrite save", File.ReadAllText("save.json") == diskSave);
    AudioSystem.Enabled = false;
    var blocked = RunUi(() => new GameManager().Run(), "1", "Locked route", "1", "", "1", "2", "", "7", "2", "3");
    Check("Route gate in menu", blocked.Contains("Defeat Brock to unlock Route 2"));

    var audioDirectory = Path.Combine(AppContext.BaseDirectory, "Audio");
    var waves = Directory.GetFiles(audioDirectory, "*.wav");
    Check("All 13 WAV files copied to output", waves.Length == 13);
    foreach (var wave in waves)
    {
        AudioSystem.ValidateWave(wave);
        Check($"Valid WAV: {Path.GetFileName(wave)}", true);
    }
    var malformed = Path.Combine(testDirectory, "broken.wav");
    File.WriteAllText(malformed, "not a wave");
    var rejected = false;
    try { AudioSystem.ValidateWave(malformed); }
    catch (InvalidDataException) { rejected = true; }
    Check("Reject invalid WAV", rejected);
    AudioSystem.Enabled = false;
    Check("Mute suppresses effects", !AudioSystem.Play(SoundEffect.Attack));
    if (args.Contains("--audio") && OperatingSystem.IsWindows())
    {
        AudioSystem.Enabled = true;
        Environment.SetEnvironmentVariable("POKEMON_AUDIO", null);
        foreach (var effect in Enum.GetValues<SoundEffect>())
        {
            Check($"Native playback accepted: {effect}", AudioSystem.Play(effect));
            Thread.Sleep(100);
        }
        Check("Native music accepted", AudioSystem.PlayBattleMusic());
        Thread.Sleep(200);
        Check("Attack plays during battle music", AudioSystem.Play(SoundEffect.Attack) && AudioSystem.IsBattleMusicPlaying);
        Check("Send-out plays during battle music", AudioSystem.Play(SoundEffect.SendOut) && AudioSystem.IsBattleMusicPlaying);
        Check("Native menu sound accepted", AudioSystem.PlayMenuSelection());
        Check("Menu sound does not stop battle music", AudioSystem.IsBattleMusicPlaying);
        AudioSystem.StopBattleMusic();
        Check("Battle music stops on request", !AudioSystem.IsBattleMusicPlaying);
        AudioSystem.Enabled = false;
        Check("Mute stops subsequent playback", !AudioSystem.PlayBattleMusic());
    }
}
catch (Exception ex)
{
    checks++;
    failures++;
    Console.WriteLine($"FAIL unexpected: {ex}");
}
finally
{
    AudioSystem.StopAll();
    Environment.SetEnvironmentVariable("POKEMON_AUDIO", originalAudio);
    Environment.CurrentDirectory = originalDirectory;
    Directory.Delete(testDirectory, recursive: true);
}
Console.WriteLine($"{checks - failures}/{checks} checks passed.");
return failures == 0 ? 0 : 1;

void Check(string name, bool condition)
{
    checks++;
    Console.WriteLine($"{(condition ? "PASS" : "FAIL")} {name}");
    if (!condition) failures++;
}

static Player NewPlayer() => new() { Name = "Tester", Party = [Pokemon.Create("bulbasaur", 5)] };

static string RunUi(Action action, params string[] input)
{
    var oldIn = Console.In;
    var oldOut = Console.Out;
    using var reader = new StringReader(string.Join("\n", input) + "\n");
    using var writer = new StringWriter();
    Console.SetIn(reader);
    Console.SetOut(writer);
    try { action(); return writer.ToString(); }
    catch (Exception ex) { throw new InvalidOperationException($"UI scenario failed. Output:\n{writer}", ex); }
    finally { Console.SetIn(oldIn); Console.SetOut(oldOut); }
}

sealed class FixedRandom(int value) : Random
{
    public override int Next(int maxValue) => Math.Clamp(value, 0, maxValue - 1);
    public override int Next(int minValue, int maxValue) => minValue + Math.Clamp(value, 0, maxValue - minValue - 1);
    public override double NextDouble() => 0;
}
