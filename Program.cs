using PokemonAdventure.Game;
using PokemonAdventure.Systems;

Console.OutputEncoding = System.Text.Encoding.UTF8;
try { new GameManager().Run(); }
catch (EndOfStreamException) { Console.WriteLine("\nInput closed. Unsaved progress was not saved."); }
finally { AudioSystem.StopAll(); }
