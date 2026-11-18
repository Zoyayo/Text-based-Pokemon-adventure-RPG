using PokemonAdventure.Models;

namespace PokemonAdventure.Data;

public static class GameData
{
    private static readonly Dictionary<string, Move> Moves = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tackle"] = Move("Tackle", PokemonType.Normal, 40, 95),
        ["Growl"] = new() { Name = "Growl", Type = PokemonType.Normal, Effect = MoveEffect.LowerAttack },
        ["Vine Whip"] = Move("Vine Whip", PokemonType.Grass, 45, 95),
        ["Razor Leaf"] = Move("Razor Leaf", PokemonType.Grass, 55, 95),
        ["Ember"] = Move("Ember", PokemonType.Fire, 40, 95),
        ["Flamethrower"] = Move("Flamethrower", PokemonType.Fire, 90),
        ["Water Gun"] = Move("Water Gun", PokemonType.Water, 40, 95),
        ["Bubble Beam"] = Move("Bubble Beam", PokemonType.Water, 65),
        ["Gust"] = Move("Gust", PokemonType.Flying, 40),
        ["Quick Attack"] = new() { Name = "Quick Attack", Type = PokemonType.Normal, Power = 45, Priority = 1 },
        ["Bug Bite"] = Move("Bug Bite", PokemonType.Bug, 40),
        ["Pin Missile"] = Move("Pin Missile", PokemonType.Bug, 55, 95),
        ["Thunder Shock"] = Move("Thunder Shock", PokemonType.Electric, 40),
        ["Thunderbolt"] = Move("Thunderbolt", PokemonType.Electric, 90),
        ["Rock Throw"] = Move("Rock Throw", PokemonType.Rock, 35, 90),
        ["Mud Slap"] = Move("Mud Slap", PokemonType.Ground, 60),
        ["Dig"] = Move("Dig", PokemonType.Ground, 60)
    };

    private static readonly Dictionary<string, PokemonSpecies> Species = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bulbasaur"] = Create("bulbasaur", "Bulbasaur", PokemonType.Grass, 20, 11, 11, 10, 45, 16, "ivysaur", (1, "Tackle"), (5, "Growl"), (7, "Vine Whip")),
        ["ivysaur"] = Create("ivysaur", "Ivysaur", PokemonType.Grass, 26, 16, 16, 14, 45, 32, "venusaur", (1, "Tackle"), (7, "Vine Whip"), (20, "Razor Leaf")),
        ["venusaur"] = Create("venusaur", "Venusaur", PokemonType.Grass, 34, 23, 23, 19, 45, null, null, (1, "Tackle"), (7, "Vine Whip"), (20, "Razor Leaf")),
        ["charmander"] = Create("charmander", "Charmander", PokemonType.Fire, 19, 12, 9, 12, 45, 16, "charmeleon", (1, "Tackle"), (5, "Growl"), (6, "Mud Slap"), (7, "Ember")),
        ["charmeleon"] = Create("charmeleon", "Charmeleon", PokemonType.Fire, 25, 17, 14, 18, 45, 36, "charizard", (1, "Tackle"), (7, "Ember"), (24, "Flamethrower")),
        ["charizard"] = Create("charizard", "Charizard", PokemonType.Fire, 33, 24, 20, 25, 45, null, null, (1, "Tackle"), (7, "Ember"), (24, "Flamethrower"), (36, "Gust")),
        ["squirtle"] = Create("squirtle", "Squirtle", PokemonType.Water, 21, 10, 13, 9, 45, 16, "wartortle", (1, "Tackle"), (5, "Growl"), (7, "Water Gun")),
        ["wartortle"] = Create("wartortle", "Wartortle", PokemonType.Water, 27, 15, 19, 13, 45, 36, "blastoise", (1, "Tackle"), (7, "Water Gun"), (20, "Bubble Beam")),
        ["blastoise"] = Create("blastoise", "Blastoise", PokemonType.Water, 35, 22, 27, 18, 45, null, null, (1, "Tackle"), (7, "Water Gun"), (20, "Bubble Beam")),
        ["pidgey"] = Create("pidgey", "Pidgey", PokemonType.Flying, 18, 10, 9, 13, 255, null, null, (1, "Tackle"), (5, "Gust"), (7, "Quick Attack")),
        ["rattata"] = Create("rattata", "Rattata", PokemonType.Normal, 17, 11, 8, 14, 255, null, null, (1, "Tackle"), (4, "Quick Attack")),
        ["caterpie"] = Create("caterpie", "Caterpie", PokemonType.Bug, 19, 8, 9, 9, 255, 7, "metapod", (1, "Tackle"), (5, "Bug Bite")),
        ["metapod"] = Create("metapod", "Metapod", PokemonType.Bug, 22, 8, 15, 6, 120, 10, "butterfree", (1, "Tackle"), (5, "Bug Bite")),
        ["butterfree"] = Create("butterfree", "Butterfree", PokemonType.Bug, 26, 18, 15, 18, 45, null, null, (1, "Tackle"), (5, "Bug Bite"), (10, "Gust")),
        ["weedle"] = Create("weedle", "Weedle", PokemonType.Bug, 18, 9, 8, 11, 255, 7, "kakuna", (1, "Tackle"), (5, "Bug Bite")),
        ["kakuna"] = Create("kakuna", "Kakuna", PokemonType.Bug, 21, 9, 14, 7, 120, 10, "beedrill", (1, "Tackle"), (5, "Bug Bite")),
        ["beedrill"] = Create("beedrill", "Beedrill", PokemonType.Bug, 27, 22, 13, 21, 45, null, null, (1, "Tackle"), (5, "Bug Bite"), (10, "Pin Missile")),
        ["pikachu"] = Create("pikachu", "Pikachu", PokemonType.Electric, 18, 13, 9, 22, 190, null, null, (1, "Thunder Shock"), (5, "Quick Attack"), (20, "Thunderbolt")),
        ["geodude"] = Create("geodude", "Geodude", PokemonType.Rock, 22, 11, 18, 5, 255, null, null, (1, "Tackle"), (6, "Rock Throw"), (8, "Mud Slap")),
        ["onix"] = Create("onix", "Onix", PokemonType.Rock, 24, 11, 21, 16, 45, null, null, (1, "Tackle"), (7, "Rock Throw"), (10, "Dig"))
    };

    public const string BoulderBadge = "Boulder Badge";
    public const int RouteOneExperienceBonus = 15;
    public static PokemonSpecies GetSpecies(string id) => Species.TryGetValue(id, out var value) ? value : throw new InvalidOperationException($"Unknown Pokémon species: {id}");
    public static Move GetMove(string name) => Moves.TryGetValue(name, out var value) ? value : throw new InvalidOperationException($"Unknown move: {name}");
    public static bool HasSpecies(string id) => Species.ContainsKey(id);
    public static bool HasMove(string name) => Moves.ContainsKey(name);
    public static IReadOnlyCollection<PokemonSpecies> AllSpecies => Species.Values;
    public static Pokemon CreateRouteOneEncounter(Random random) => Pokemon.Create(random.Next(100) < 55 ? "rattata" : "pidgey", random.Next(2, 6));

    public static Pokemon CreateRouteTwoEncounter(Random random)
    {
        var roll = random.Next(100);
        var species = roll < 30 ? "caterpie" : roll < 60 ? "weedle" : roll < 80 ? "pidgey" : "pikachu";
        return Pokemon.Create(species, random.Next(5, 10));
    }

    public static List<Pokemon> CreateBrockTeam() => [Pokemon.Create("geodude", 6), Pokemon.Create("onix", 7)];

    private static Move Move(string name, PokemonType type, int power, int accuracy = 100) => new() { Name = name, Type = type, Power = power, Accuracy = accuracy };
    private static PokemonSpecies Create(string id, string name, PokemonType type, int hp, int attack, int defense, int speed, int catchRate, int? evolutionLevel, string? target, params (int Level, string Move)[] learnset) => new()
    {
        Id = id, Name = name, Type = type, BaseHp = hp, BaseAttack = attack, BaseDefense = defense, BaseSpeed = speed, CatchRate = catchRate,
        EvolutionLevel = evolutionLevel, EvolutionTargetId = target, Learnset = learnset.ToDictionary(entry => entry.Level, entry => entry.Move)
    };
}
