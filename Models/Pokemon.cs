using System.Text.Json.Serialization;
using PokemonAdventure.Data;

namespace PokemonAdventure.Models;

public enum PokemonProgress { LevelUp, Evolution }

public sealed class Pokemon
{
    internal const int MaxLevel = 100;
    internal const int MaxMoves = 4;

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
        if (level is < 1 or > MaxLevel) throw new ArgumentOutOfRangeException(nameof(level));
        var species = GameData.GetSpecies(speciesId);
        var pokemon = new Pokemon { SpeciesId = speciesId, Level = level };
        pokemon.CurrentHp = pokemon.MaxHp;
        pokemon.MoveNames = MovesThroughLevel(species, level).TakeLast(MaxMoves).ToList();
        return pokemon;
    }

    public void Heal() => CurrentHp = MaxHp;

    public void ReceiveExperience(int amount, Action<string> message, Action<PokemonProgress>? progress = null)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (Level == MaxLevel)
        {
            Experience = 0;
            return;
        }
        Experience = checked(Experience + amount);
        message($"{Name} gained {amount} EXP!");
        while (Level < MaxLevel && Experience >= ExperienceToLevel)
        {
            Experience -= ExperienceToLevel;
            Level++;
            CurrentHp = Math.Min(MaxHp, CurrentHp + 5);
            progress?.Invoke(PokemonProgress.LevelUp);
            message($"{Name} reached Lv. {Level}! Stats increased.");
            if (Species.Learnset.TryGetValue(Level, out var moveName))
                LearnMove(moveName, message);
            EvolveIfReady(message, progress);
        }
        if (Level == MaxLevel) Experience = 0;
    }

    private void EvolveIfReady(Action<string> message, Action<PokemonProgress>? progress)
    {
        while (Species.EvolutionLevel is int required && Level >= required && Species.EvolutionTargetId is string target)
        {
            var oldName = Name;
            var oldMaxHp = MaxHp;
            SpeciesId = target;
            CurrentHp = Math.Min(MaxHp, CurrentHp + MaxHp - oldMaxHp);
            progress?.Invoke(PokemonProgress.Evolution);
            message($"{oldName} evolved into {Name}!");
            foreach (var move in MovesThroughLevel(Species, Level))
                LearnMove(move, message);
        }
    }

    private void LearnMove(string moveName, Action<string> message)
    {
        if (MoveNames.Contains(moveName)) return;
        if (MoveNames.Count == MaxMoves) MoveNames.RemoveAt(0);
        MoveNames.Add(moveName);
        message($"{Name} learned {moveName}!");
    }

    private static IEnumerable<string> MovesThroughLevel(PokemonSpecies species, int level) =>
        species.Learnset.Where(entry => entry.Key <= level)
            .OrderBy(entry => entry.Key)
            .Select(entry => entry.Value);
}
