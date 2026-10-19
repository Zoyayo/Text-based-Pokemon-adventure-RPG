namespace PokemonAdventure.Models;

public sealed class PokemonSpecies
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public PokemonType Type { get; init; }
    public int BaseHp { get; init; }
    public int BaseAttack { get; init; }
    public int BaseDefense { get; init; }
    public int BaseSpeed { get; init; }
    public int CatchRate { get; init; }
    public Dictionary<int, string> Learnset { get; init; } = [];
}
