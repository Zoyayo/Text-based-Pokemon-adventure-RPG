using PokemonAdventure.Models;

namespace PokemonAdventure.Data;

public static class GameData
{
    private static readonly Dictionary<string, Move> Moves = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tackle"] = new() { Name = "Tackle", Type = PokemonType.Normal, Power = 40, Accuracy = 95 },
        ["Growl"] = new() { Name = "Growl", Type = PokemonType.Normal, Power = 20, Accuracy = 100 },
        ["Vine Whip"] = new() { Name = "Vine Whip", Type = PokemonType.Grass, Power = 45, Accuracy = 95 },
        ["Ember"] = new() { Name = "Ember", Type = PokemonType.Fire, Power = 40, Accuracy = 95 },
        ["Water Gun"] = new() { Name = "Water Gun", Type = PokemonType.Water, Power = 40, Accuracy = 95 },
        ["Gust"] = new() { Name = "Gust", Type = PokemonType.Flying, Power = 40, Accuracy = 100 },
        ["Quick Attack"] = new() { Name = "Quick Attack", Type = PokemonType.Normal, Power = 45, Accuracy = 100 }
    };

    private static readonly Dictionary<string, PokemonSpecies> Species = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bulbasaur"] = Create("bulbasaur", "Bulbasaur", PokemonType.Grass, 20, 11, 11, 10, 45, (1, "Tackle"), (5, "Growl"), (7, "Vine Whip")),
        ["charmander"] = Create("charmander", "Charmander", PokemonType.Fire, 19, 12, 9, 12, 45, (1, "Tackle"), (5, "Growl"), (7, "Ember")),
        ["squirtle"] = Create("squirtle", "Squirtle", PokemonType.Water, 21, 10, 13, 9, 45, (1, "Tackle"), (5, "Growl"), (7, "Water Gun")),
        ["pidgey"] = Create("pidgey", "Pidgey", PokemonType.Flying, 18, 10, 9, 13, 255, (1, "Tackle"), (5, "Gust"), (7, "Quick Attack")),
        ["rattata"] = Create("rattata", "Rattata", PokemonType.Normal, 17, 11, 8, 14, 255, (1, "Tackle"), (4, "Quick Attack"))
    };

    public static PokemonSpecies GetSpecies(string id) => Species.TryGetValue(id, out var value) ? value : throw new InvalidOperationException($"Unknown Pokémon species: {id}");
    public static Move GetMove(string name) => Moves.TryGetValue(name, out var value) ? value : throw new InvalidOperationException($"Unknown move: {name}");
    public static IReadOnlyCollection<PokemonSpecies> AllSpecies => Species.Values;
    public static Pokemon CreateRouteOneEncounter(Random random) => Pokemon.Create(random.Next(100) < 55 ? "rattata" : "pidgey", random.Next(2, 6));

    private static PokemonSpecies Create(string id, string name, PokemonType type, int hp, int attack, int defense, int speed, int catchRate, params (int Level, string Move)[] learnset) => new()
    {
        Id = id, Name = name, Type = type, BaseHp = hp, BaseAttack = attack, BaseDefense = defense, BaseSpeed = speed, CatchRate = catchRate,
        Learnset = learnset.ToDictionary(entry => entry.Level, entry => entry.Move)
    };
}
