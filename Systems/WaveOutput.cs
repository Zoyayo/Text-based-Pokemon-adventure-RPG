using System.Runtime.InteropServices;

namespace PokemonAdventure.Systems;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct WaveFormat
{
    public ushort Encoding;
    public ushort Channels;
    public uint SampleRate;
    public uint ByteRate;
    public ushort BlockAlign;
    public ushort Bits;
    public ushort ExtraSize;
}

internal sealed class WaveOutput
{
    private const int Done = 1;
    private const uint LoopFlags = 4 | 8;
    private static readonly uint HeaderSize = (uint)Marshal.SizeOf<WaveHeader>();
    private static readonly int FlagsOffset = Marshal.OffsetOf<WaveHeader>(nameof(WaveHeader.Flags)).ToInt32();
    private IntPtr _device;
    private IntPtr _header;
    private GCHandle _samples;
    private bool _prepared;

    public bool IsPlaying => _prepared && (Marshal.ReadInt32(_header, FlagsOffset) & Done) == 0;

    public void Play(WaveFormat format, byte[] samples, bool loop)
    {
        Stop();
        if (_device != IntPtr.Zero) throw new IOException("The previous audio buffer could not be released.");

        Check(waveOutOpen(out var device, uint.MaxValue, ref format, IntPtr.Zero, IntPtr.Zero, 0), "open audio device");
        _device = device;
        try
        {
            // Windows keeps both addresses until playback finishes or the device is reset.
            _samples = GCHandle.Alloc(samples, GCHandleType.Pinned);
            _header = Marshal.AllocHGlobal((int)HeaderSize);
            var header = new WaveHeader { Data = _samples.AddrOfPinnedObject(), Length = (uint)samples.Length };
            Marshal.StructureToPtr(header, _header, false);
            Check(waveOutPrepareHeader(_device, _header, HeaderSize), "prepare audio buffer");
            _prepared = true;
            if (loop)
            {
                header.Flags = (uint)Marshal.ReadInt32(_header, FlagsOffset) | LoopFlags;
                header.Loops = uint.MaxValue;
                Marshal.StructureToPtr(header, _header, false);
            }
            Check(waveOutWrite(_device, _header, HeaderSize), "start playback");
        }
        catch
        {
            Stop();
            throw;
        }
    }

    public void Stop()
    {
        if (_device == IntPtr.Zero) return;
        if (_prepared)
        {
            if (IsPlaying) waveOutReset(_device);
            if (waveOutUnprepareHeader(_device, _header, HeaderSize) != 0) return;
            _prepared = false;
        }
        Marshal.FreeHGlobal(_header);
        _header = IntPtr.Zero;
        if (_samples.IsAllocated) _samples.Free();
        if (waveOutClose(_device) == 0) _device = IntPtr.Zero;
    }

    private static void Check(uint result, string action)
    {
        if (result != 0) throw new IOException($"Windows could not {action} (error {result}).");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveHeader
    {
        public IntPtr Data;
        public uint Length;
        public uint BytesRecorded;
        public UIntPtr User;
        public uint Flags;
        public uint Loops;
        public IntPtr Next;
        public UIntPtr Reserved;
    }

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint waveOutOpen(out IntPtr device, uint deviceId, ref WaveFormat format, IntPtr callback, IntPtr instance, uint flags);

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint waveOutPrepareHeader(IntPtr device, IntPtr header, uint size);

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint waveOutWrite(IntPtr device, IntPtr header, uint size);

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint waveOutReset(IntPtr device);

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint waveOutUnprepareHeader(IntPtr device, IntPtr header, uint size);

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint waveOutClose(IntPtr device);
}
