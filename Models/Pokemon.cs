using System.Text.Json.Serialization;
using PokemonAdventure.Data;

namespace PokemonAdventure.Models;

public sealed class Pokemon
{
    public string SpeciesId { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Experience { get; set; }
    public int CurrentHp { get; set; }
    public List<string> MoveNames { get; set; } = [];

    [JsonIgnore] public PokemonSpecies Species => GameData.GetSpecies(SpeciesId);
    [JsonIgnore] public string Name => Species.Name;
    [JsonIgnore] public int MaxHp => Species.BaseHp + Level * 3;
    [JsonIgnore] public int Attack => Species.BaseAttack + Level * 2;
    [JsonIgnore] public int Defense => Species.BaseDefense + Level * 2;
    [JsonIgnore] public int Speed => Species.BaseSpeed + Level * 2;
    [JsonIgnore] public bool IsFainted => CurrentHp <= 0;
    [JsonIgnore] public int ExperienceToLevel => Level * 25;

    public static Pokemon Create(string speciesId, int level)
    {
        var species = GameData.GetSpecies(speciesId);
        var pokemon = new Pokemon { SpeciesId = speciesId, Level = level };
        pokemon.CurrentHp = pokemon.MaxHp;
        pokemon.MoveNames = species.Learnset.Where(entry => entry.Key <= level).OrderBy(entry => entry.Key).Select(entry => entry.Value).TakeLast(4).ToList();
        return pokemon;
    }

    public void Heal() => CurrentHp = MaxHp;

    public void ReceiveExperience(int amount, Action<string> message)
    {
        Experience += amount;
        message($"{Name} gained {amount} EXP!");
        while (Level < 100 && Experience >= ExperienceToLevel)
        {
            Experience -= ExperienceToLevel;
            Level++;
            CurrentHp = Math.Min(MaxHp, CurrentHp + 5);
            message($"{Name} reached Lv. {Level}! Stats increased.");
            if (Species.Learnset.TryGetValue(Level, out var moveName) && !MoveNames.Contains(moveName))
            {
                if (MoveNames.Count == 4) MoveNames.RemoveAt(0);
                MoveNames.Add(moveName);
                message($"{Name} learned {moveName}!");
            }
        }
    }
}
