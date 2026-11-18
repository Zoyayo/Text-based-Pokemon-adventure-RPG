using PokemonAdventure.Data;
using PokemonAdventure.Models;

namespace PokemonAdventure.Systems;

public enum ExplorationEvent { WildPokemon, Item, Nothing }

public static class ExplorationSystem
{
    public static bool CanExploreRoute(Player player, int route) =>
        route == 1 || (route == 2 && player.Badges.Contains(GameData.BoulderBadge));

    public static ExplorationEvent RollEvent(Random random)
    {
        var roll = random.Next(100);
        return roll < 65 ? ExplorationEvent.WildPokemon : roll < 90 ? ExplorationEvent.Item : ExplorationEvent.Nothing;
    }

    public static (string Item, int Amount) FindItem(Player player, Random random)
    {
        var item = random.Next(2) == 0 ? Player.PokeBall : Player.Potion;
        var amount = AddItem(player, item, random.Next(1, 3));
        return (item, amount);
    }

    public static int AddItem(Player player, string item, int amount)
    {
        var current = player.Bag.GetValueOrDefault(item);
        var added = Math.Min(amount, int.MaxValue - current);
        player.Bag[item] = current + added;
        return added;
    }

    public static bool AwardBrockVictory(Player player)
    {
        if (!player.Badges.Add(GameData.BoulderBadge)) return false;
        AddItem(player, Player.PokeBall, 5);
        AddItem(player, Player.Potion, 2);
        return true;
    }
}
