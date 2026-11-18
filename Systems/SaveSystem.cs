using System.Text.Json;
using PokemonAdventure.Data;
using PokemonAdventure.Models;

namespace PokemonAdventure.Systems;

public readonly record struct SaveResult(bool Success, string Message);
public readonly record struct LoadResult(Player? Player, string Message, bool RecoveredBackup = false);

public static class SaveSystem
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private static string DefaultPath => Path.Combine(Environment.CurrentDirectory, "save.json");

    public static bool SaveExists() => File.Exists(DefaultPath) || File.Exists(DefaultPath + ".bak");
    public static string Snapshot(Player player) => JsonSerializer.Serialize(player, Options);

    public static SaveResult Save(Player player, string? path = null)
    {
        path ??= DefaultPath;
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            ValidateAndNormalize(player);
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, player, Options);
                stream.Flush(flushToDisk: true);
            }
            // Keep the last valid save. A corrupt primary must never replace a good backup.
            if (File.Exists(path) && ReadValid(path) is not null)
                File.Replace(temporaryPath, path, path + ".bak");
            else
                File.Move(temporaryPath, path, overwrite: true);
            return new(true, "Game saved to save.json.");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException)
        {
            return new(false, $"Could not save: {ex.Message}. Your progress remains in memory.");
        }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public static LoadResult Load(string? path = null)
    {
        path ??= DefaultPath;
        var player = ReadValid(path);
        if (player is not null) return new(player, "Game loaded.");
        player = ReadValid(path + ".bak");
        if (player is not null) return new(player, "Primary save missing or invalid. Loaded save.json.bak; save again to restore the primary.", true);
        return new(null, "No valid save or backup could be read. Files were left unchanged.");
    }

    private static Player? ReadValid(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var player = JsonSerializer.Deserialize<Player>(File.ReadAllText(path), Options);
            if (player is null) return null;
            ValidateAndNormalize(player);
            return player;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private static void ValidateAndNormalize(Player player)
    {
        if (string.IsNullOrWhiteSpace(player.Name) || player.Party is null ||
            player.Party.Count is < 1 or > Player.MaxPartySize ||
            player.Storage is null || player.Bag is null || player.SeenSpecies is null ||
            player.CaughtSpecies is null || player.Badges is null)
            throw new InvalidDataException("Incomplete player data.");
        if (player.Bag.Any(item => string.IsNullOrWhiteSpace(item.Key) || item.Value < 0) ||
            player.SeenSpecies.Concat(player.CaughtSpecies).Any(id => string.IsNullOrEmpty(id) || !GameData.HasSpecies(id)) ||
            player.Badges.Any(badge => badge != GameData.BoulderBadge))
            throw new InvalidDataException("Invalid inventory or Pokédex.");
        foreach (var pokemon in player.Party.Concat(player.Storage))
        {
            if (pokemon is null || string.IsNullOrEmpty(pokemon.SpeciesId) || !GameData.HasSpecies(pokemon.SpeciesId) ||
                pokemon.Level is < 1 or > Pokemon.MaxLevel || pokemon.Experience < 0 ||
                (pokemon.Level < Pokemon.MaxLevel && pokemon.Experience >= pokemon.ExperienceToLevel) ||
                pokemon.CurrentHp < 0 || pokemon.CurrentHp > pokemon.MaxHp ||
                pokemon.MoveNames is null || pokemon.MoveNames.Count is < 1 or > Pokemon.MaxMoves ||
                pokemon.MoveNames.Any(move => string.IsNullOrEmpty(move) || !GameData.HasMove(move)))
                throw new InvalidDataException("Invalid Pokémon data.");
            if (pokemon.Level == Pokemon.MaxLevel) pokemon.Experience = 0;
            player.RegisterCaughtSpecies(pokemon.SpeciesId);
        }
        player.SeenSpecies.UnionWith(player.CaughtSpecies);
    }
}
