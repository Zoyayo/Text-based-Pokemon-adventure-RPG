using PokemonAdventure.Models;
using PokemonAdventure.Systems;

namespace PokemonAdventure.Game;

public sealed class PokemonManagementMenu(Player player)
{
    public void ShowParty()
    {
        while (true)
        {
            ConsoleUi.Clear();
            PrintPokemon("YOUR PARTY", player.Party);
            Console.WriteLine("The first healthy Pokémon leads battles.");
            Console.WriteLine("1. Change party order  2. Storage  0. Back");
            switch (ConsoleUi.ReadChoice(0, 2))
            {
                case 0: return;
                case 1:
                    var first = SelectPokemon("Choose first slot", player.Party);
                    if (first < 0) break;
                    var second = SelectPokemon("Choose second slot", player.Party);
                    if (second >= 0) ShowResult(PartySystem.SwapOrder(player, first, second));
                    break;
                case 2: ShowStorage(); break;
            }
        }
    }

    public void ShowStorage()
    {
        while (true)
        {
            ConsoleUi.Clear();
            PrintPokemon("PARTY", player.Party);
            PrintPokemon("STORAGE", player.Storage);
            Console.WriteLine("1. Deposit  2. Withdraw  3. Exchange  0. Back");
            switch (ConsoleUi.ReadChoice(0, 3))
            {
                case 0: return;
                case 1:
                    var deposit = SelectPokemon("Deposit from party", player.Party);
                    if (deposit >= 0) ShowResult(PartySystem.Deposit(player, deposit));
                    break;
                case 2:
                    var withdraw = SelectPokemon("Withdraw from storage", player.Storage);
                    if (withdraw >= 0) ShowResult(PartySystem.Withdraw(player, withdraw));
                    break;
                case 3:
                    if (player.Storage.Count == 0) { ShowResult(new(false, "Storage is empty.")); break; }
                    var party = SelectPokemon("Exchange: choose party Pokémon", player.Party);
                    if (party < 0) break;
                    var storage = SelectPokemon("Exchange: choose storage Pokémon", player.Storage);
                    if (storage >= 0) ShowResult(PartySystem.Exchange(player, party, storage));
                    break;
            }
        }
    }

    private static int SelectPokemon(string title, List<Pokemon> pokemon)
    {
        if (pokemon.Count == 0) { ShowResult(new(false, "No Pokémon here.")); return -1; }
        ConsoleUi.Clear();
        PrintPokemon(title, pokemon);
        Console.WriteLine("0. Cancel");
        return ConsoleUi.ReadChoice(0, pokemon.Count) - 1;
    }

    private static void PrintPokemon(string title, List<Pokemon> pokemon)
    {
        Console.WriteLine($"\n{title} ({pokemon.Count})");
        if (pokemon.Count == 0) Console.WriteLine("(empty)");
        for (var i = 0; i < pokemon.Count; i++)
        {
            var p = pokemon[i];
            Console.WriteLine($"{i + 1}. {p.Name} Lv. {p.Level} [{p.Species.Type}] HP {p.CurrentHp}/{p.MaxHp} EXP {p.Experience}/{p.ExperienceToLevel}");
            Console.WriteLine($"   Moves: {string.Join(", ", p.MoveNames)}");
        }
    }

    private static void ShowResult(PartyResult result)
    {
        Console.WriteLine(result.Message);
        ConsoleUi.Pause();
    }
}
