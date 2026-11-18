using PokemonAdventure.Models;

namespace PokemonAdventure.Systems;

public readonly record struct PartyResult(bool Success, string Message);

public static class PartySystem
{
    public static PartyResult Deposit(Player player, int index)
    {
        if (!Valid(player.Party, index)) return new(false, "Invalid party selection.");
        if (player.Party.Count <= 1) return new(false, "Keep at least one Pokémon in your party.");
        var pokemon = player.Party[index];
        player.Party.RemoveAt(index);
        player.Storage.Add(pokemon);
        return new(true, $"{pokemon.Name} was deposited in Storage.");
    }

    public static PartyResult Withdraw(Player player, int index)
    {
        if (!Valid(player.Storage, index)) return new(false, "Invalid storage selection.");
        if (player.Party.Count >= Player.MaxPartySize) return new(false, "Party is full. Use Exchange instead.");
        var pokemon = player.Storage[index];
        player.Storage.RemoveAt(index);
        player.Party.Add(pokemon);
        return new(true, $"{pokemon.Name} joined your party.");
    }

    public static PartyResult Exchange(Player player, int partyIndex, int storageIndex)
    {
        if (!Valid(player.Party, partyIndex) || !Valid(player.Storage, storageIndex))
            return new(false, "Invalid Pokémon selection.");
        (player.Party[partyIndex], player.Storage[storageIndex]) = (player.Storage[storageIndex], player.Party[partyIndex]);
        return new(true, "Party and Storage Pokémon exchanged.");
    }

    public static PartyResult SwapOrder(Player player, int first, int second)
    {
        if (!Valid(player.Party, first) || !Valid(player.Party, second)) return new(false, "Invalid party selection.");
        if (first == second) return new(false, "Choose two different slots.");
        (player.Party[first], player.Party[second]) = (player.Party[second], player.Party[first]);
        return new(true, $"Party slots {first + 1} and {second + 1} swapped.");
    }

    private static bool Valid(List<Pokemon> list, int index) => index >= 0 && index < list.Count;
}
