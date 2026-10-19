using System.Text.Json;
using PokemonAdventure.Models;

namespace PokemonAdventure.Systems;

public static class SaveSystem
{
    private static readonly string SavePath = Path.Combine(Environment.CurrentDirectory, "save.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static bool SaveExists() => File.Exists(SavePath);
    public static void Save(Player player) => File.WriteAllText(SavePath, JsonSerializer.Serialize(player, Options));

    public static Player? Load()
    {
        if (!SaveExists()) return null;
        try { return JsonSerializer.Deserialize<Player>(File.ReadAllText(SavePath), Options); }
        catch (JsonException) { return null; }
    }
}
