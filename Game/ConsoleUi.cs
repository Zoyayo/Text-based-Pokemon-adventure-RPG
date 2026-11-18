using PokemonAdventure.Systems;

namespace PokemonAdventure.Game;

public static class ConsoleUi
{
    public static void Clear()
    {
        if (Console.IsOutputRedirected) return;
        try { Console.Clear(); }
        catch (IOException) { Console.WriteLine(); }
    }

    public static string ReadLine() => Console.ReadLine() ?? throw new EndOfStreamException();

    public static int ReadChoice(int min, int max, bool menuSound = true)
    {
        while (true)
        {
            if (int.TryParse(ReadLine(), out var choice) && choice >= min && choice <= max)
            {
                if (menuSound) AudioSystem.PlayMenuSelection();
                return choice;
            }
            Console.Write($"Choose {min}-{max}: ");
        }
    }

    public static void Pause()
    {
        Console.WriteLine("\nPress Enter to continue...");
        ReadLine();
    }
}
