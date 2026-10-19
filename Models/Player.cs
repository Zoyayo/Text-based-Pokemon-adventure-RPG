namespace PokemonAdventure.Models;

public sealed class Player
{
    public string Name { get; set; } = string.Empty;
    public List<Pokemon> Party { get; set; } = [];
    public List<Pokemon> Storage { get; set; } = [];
    public Dictionary<string, int> Bag { get; set; } = new() { ["Poké Ball"] = 8, ["Potion"] = 3 };
    public HashSet<string> SeenSpecies { get; set; } = [];
    public HashSet<string> CaughtSpecies { get; set; } = [];
    public Pokemon? FirstAvailablePokemon() => Party.FirstOrDefault(pokemon => !pokemon.IsFainted);
    public bool HasUsablePokemon() => FirstAvailablePokemon() is not null;
}
