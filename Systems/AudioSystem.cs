using System.Buffers.Binary;
using System.Text;

namespace PokemonAdventure.Systems;

public enum SoundEffect { Encounter, Attack, Heal, Caught, ItemFound, ItemReceived, LevelUp, Evolution, RunAway, Save, Victory, SendOut }

public static class AudioSystem
{
    private static readonly WaveOutput Music = new();
    private static readonly WaveOutput Effects = new();
    private static readonly WaveOutput Menu = new();
    private static bool _enabled = true;
    private static readonly HashSet<string> ReportedErrors = [];
    private static readonly Dictionary<SoundEffect, string> Files = new()
    {
        [SoundEffect.Encounter] = "pokemon-exclamation-mark-sound-effect.wav",
        [SoundEffect.Attack] = "pokemon-sound.wav",
        [SoundEffect.Heal] = "111-pokemon-recovery.wav",
        [SoundEffect.Caught] = "06-caught-a-pokemon.wav",
        [SoundEffect.ItemFound] = "pokemon-item-found-sound-effect.wav",
        [SoundEffect.ItemReceived] = "129-received-an-item.wav",
        [SoundEffect.LevelUp] = "pokemon-level-up-sound-effect.wav",
        [SoundEffect.Evolution] = "pokemon-evolve.wav",
        [SoundEffect.RunAway] = "pokemon-run-away-sound-effect-1.wav",
        [SoundEffect.Save] = "pokemon-save-game-sound-effect.wav",
        [SoundEffect.Victory] = "pokemon-wild-pokemon-victory-theme-1.wav",
        [SoundEffect.SendOut] = "sound-effects-pokemon-out.wav"
    };

    public static bool Enabled
    {
        get => _enabled;
        set { _enabled = value; if (!value) StopAll(); }
    }

    internal static bool IsBattleMusicPlaying => Music.IsPlaying;

    public static bool Play(SoundEffect effect) =>
        PlayFile(Effects, Files[effect], effect == SoundEffect.Attack ? 0.5 : 0.65);

    public static bool PlayBattleMusic() =>
        CanPlay && (IsBattleMusicPlaying || PlayFile(Music, "wild-pokemon-battle.wav", 0.30, loop: true));

    // Ignore repeats while the short confirmation is still playing.
    public static bool PlayMenuSelection() =>
        CanPlay && (Menu.IsPlaying || PlayFile(Menu, "pokemon-exclamation-mark-sound-effect.wav", 0.22, menuCue: true));

    private static bool CanPlay => Enabled && OperatingSystem.IsWindows() &&
        Environment.GetEnvironmentVariable("POKEMON_AUDIO") != "0";

    private static bool PlayFile(WaveOutput output, string name, double volume, bool loop = false, bool menuCue = false)
    {
        if (!CanPlay) return false;
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Audio", name);
            using var stream = File.OpenRead(path);
            var (format, length) = ReadWave(stream);
            if (menuCue) length = Math.Min(length, Math.Max(1, (int)(format.SampleRate * 180L / 1000)) * format.BlockAlign);
            var samples = new byte[length];
            stream.ReadExactly(samples);
            AdjustVolume(samples, volume, format.BlockAlign, menuCue ? (int)(format.SampleRate * 55L / 1000) : 0);
            output.Play(format, samples, loop);
            return true;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or DllNotFoundException or EntryPointNotFoundException)
        {
            if (ReportedErrors.Add(name)) Console.Error.WriteLine($"Audio unavailable: {name}. {ex.Message}");
            return false;
        }
    }

    public static void StopBattleMusic() => Music.Stop();

    public static void StopAll()
    {
        StopBattleMusic();
        Effects.Stop();
        Menu.Stop();
    }

    public static void ValidateWave(string path)
    {
        using var stream = File.OpenRead(path);
        ReadWave(stream);
    }

    private static (WaveFormat Format, int Length) ReadWave(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);
        if (stream.Length < 44 || Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF")
            throw new InvalidDataException("Expected a RIFF WAV file.");
        var end = (long)reader.ReadUInt32() + 8;
        if (end != stream.Length || Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE")
            throw new InvalidDataException("Invalid WAV length or header.");
        var format = new WaveFormat();
        uint dataLength = 0;
        long dataStart = 0;
        while (stream.Position + 8 <= end)
        {
            var tag = Encoding.ASCII.GetString(reader.ReadBytes(4));
            var length = reader.ReadUInt32();
            var next = stream.Position + length + (length % 2);
            if (next > end) throw new InvalidDataException("Truncated WAV chunk.");
            if (tag == "fmt ")
            {
                if (length < 16) throw new InvalidDataException("Invalid WAV format.");
                format.Encoding = reader.ReadUInt16();
                format.Channels = reader.ReadUInt16();
                format.SampleRate = reader.ReadUInt32();
                format.ByteRate = reader.ReadUInt32();
                format.BlockAlign = reader.ReadUInt16();
                format.Bits = reader.ReadUInt16();
            }
            else if (tag == "data")
            {
                dataStart = stream.Position;
                dataLength = length;
            }
            stream.Position = next;
        }
        if (format.Encoding != 1 || format.Channels is not (1 or 2) || format.Bits != 16 ||
            format.SampleRate == 0 || format.BlockAlign != format.Channels * 2 ||
            format.ByteRate != (long)format.SampleRate * format.BlockAlign ||
            dataLength == 0 || dataLength > int.MaxValue || dataLength % format.BlockAlign != 0)
            throw new InvalidDataException("Expected non-empty 16-bit PCM audio.");
        stream.Position = dataStart;
        return (format, (int)dataLength);
    }

    private static void AdjustVolume(Span<byte> samples, double volume, int blockAlign, int fadeFrames)
    {
        var frames = samples.Length / blockAlign;
        fadeFrames = Math.Min(frames, fadeFrames);
        var fadeStart = frames - fadeFrames;
        for (var i = 0; i < samples.Length; i += 2)
        {
            var frame = i / blockAlign;
            var gain = frame < fadeStart ? volume : volume * (frames - frame - 1) / Math.Max(1, fadeFrames - 1);
            var sample = BinaryPrimitives.ReadInt16LittleEndian(samples[i..]);
            BinaryPrimitives.WriteInt16LittleEndian(samples[i..], (short)(sample * gain));
        }
    }
}
