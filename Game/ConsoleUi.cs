namespace PokemonAdventure.Game;

public static class ConsoleUi
{
    public static void Clear()
    {
        try { Console.Clear(); }
        catch (IOException) { Console.WriteLine(); }
    }
}
