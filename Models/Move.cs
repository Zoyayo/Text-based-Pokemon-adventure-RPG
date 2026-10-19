namespace PokemonAdventure.Models;

public enum PokemonType { Normal, Grass, Fire, Water, Flying }

public sealed class Move
{
    public string Name { get; init; } = "Tackle";
    public PokemonType Type { get; init; }
    public int Power { get; init; }
    public int Accuracy { get; init; } = 100;
}
