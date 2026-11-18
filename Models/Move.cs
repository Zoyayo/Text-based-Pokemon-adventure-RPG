namespace PokemonAdventure.Models;

public enum PokemonType { Normal, Grass, Fire, Water, Flying, Electric, Bug, Rock, Ground }
public enum MoveEffect { Damage, LowerAttack }

public sealed class Move
{
    public string Name { get; init; } = "Tackle";
    public PokemonType Type { get; init; }
    public int Power { get; init; }
    public int Accuracy { get; init; } = 100;
    public MoveEffect Effect { get; init; }
    public int Priority { get; init; }
}
