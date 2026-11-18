namespace PokemonAdventure.Models;

public sealed class Player
{
    public const int MaxPartySize = 6;
    public const string PokeBall = "Poké Ball";
    public const string Potion = "Potion";

    public string Name { get; set; } = string.Empty;
    public List<Pokemon> Party { get; set; } = [];
    public List<Pokemon> Storage { get; set; } = [];
    public Dictionary<string, int> Bag { get; set; } = new() { [PokeBall] = 8, [Potion] = 3 };
    public HashSet<string> SeenSpecies { get; set; } = [];
    public HashSet<string> CaughtSpecies { get; set; } = [];
    public HashSet<string> Badges { get; set; } = [];
    public bool SoundEnabled { get; set; } = true;
    public Pokemon? FirstAvailablePokemon() => Party.FirstOrDefault(pokemon => !pokemon.IsFainted);
    public bool HasUsablePokemon() => FirstAvailablePokemon() is not null;

    internal void RegisterCaughtSpecies(string speciesId)
    {
        SeenSpecies.Add(speciesId);
        CaughtSpecies.Add(speciesId);
    }
}
