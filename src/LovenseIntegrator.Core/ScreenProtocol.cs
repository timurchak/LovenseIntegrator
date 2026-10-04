using System.Buffers.Binary;

namespace LovenseIntegrator.Core;

public sealed record ScreenEventDefinition(byte Code, string Id, string Name, string Description);
public static class ScreenEvents
{
    // IDs and wire codes are permanent. Never localize or renumber them.
    public static IReadOnlyList<ScreenEventDefinition> All { get; } =
    [
        new(1, "wow.combat.enter", "Combat started", "The player entered combat."),
        new(2, "wow.combat.leave", "Combat ended", "The player left combat."),
        new(3, "wow.player.damage", "Damage received", "A player damage notification. Available only when WoW exposes its event type."),
        new(4, "wow.player.heal", "Healing received", "A player healing notification. Available only when WoW exposes its event type."),
        new(5, "wow.player.health_loss", "Health lost", "Health decreased. Value is percentage points lost, not damage dealt. Requires readable health values."),
        new(6, "wow.player.low_health", "Low health", "Health crossed below 30%. Rearms above 35%. Requires readable health values."),
        new(7, "wow.player.dead", "Player died", "The player died."),
        new(8, "wow.player.resurrected", "Player resurrected", "The player returned to life, excluding ghost release."),
        new(9, "wow.encounter.start", "Boss encounter started", "A boss encounter started."),
        new(10, "wow.encounter.win", "Boss defeated", "A boss encounter ended successfully."),
        new(11, "wow.encounter.wipe", "Boss attempt failed", "A boss encounter ended unsuccessfully."),
        new(12, "wow.quest.complete", "Quest completed", "A quest was turned in."),
        new(13, "wow.level.up", "Level gained", "The player gained a level."),
        new(14, "wow.test", "Test signal", "A diagnostic addon signal. It is shown in the preview and never triggers an action.")
    ];
    public static ScreenEventDefinition? Find(string id) => All.FirstOrDefault(e => e.Id == id);
    public static ScreenEventDefinition? Find(byte code) => All.FirstOrDefault(e => e.Code == code);
}

public sealed record ScreenPacket(uint Session, ushort Sequence, byte EventCode, byte Flags, ushort Value, ushort Heartbeat)
{
    public bool IsTest => (Flags & 1) != 0 || EventCode == 14;
    public bool Restricted => (Flags & 2) != 0;
}

// 8x8 cells, RGB bits in row-major order, most significant bit first: 24 bytes.
public static class ScreenProtocol
{
    public const int Cells = 8;
    public static byte[] Encode(ScreenPacket packet)
    {
        var bytes = new byte[24];
        bytes[0] = 87; bytes[1] = 83; bytes[2] = 69; bytes[3] = 1;
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(4), packet.Session);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(8), packet.Sequence);
        bytes[10] = packet.EventCode; bytes[11] = packet.Flags;
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(12), packet.Value);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(14), packet.Heartbeat);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(22), Crc(bytes.AsSpan(0, 22)));
        return bytes;
    }
    public static ushort Crc(ReadOnlySpan<byte> bytes)
    {
        ushort crc = 0xffff;
        foreach (var b in bytes)
        {
            crc ^= (ushort)(b << 8);
            for (var bit = 0; bit < 8; bit++) crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
        }
        return crc;
    }
    public static ScreenPacket? Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 24 || bytes[0] != 87 || bytes[1] != 83 || bytes[2] != 69 || bytes[3] != 1 ||
            (bytes[11] & ~3) != 0 || bytes.Slice(16, 6).ContainsAnyExcept((byte)0) ||
            BinaryPrimitives.ReadUInt16BigEndian(bytes[22..]) != Crc(bytes[..22]) ||
            bytes[10] != 0 && ScreenEvents.Find(bytes[10]) is null) return null;
        return new(BinaryPrimitives.ReadUInt32BigEndian(bytes[4..]), BinaryPrimitives.ReadUInt16BigEndian(bytes[8..]),
            bytes[10], bytes[11], BinaryPrimitives.ReadUInt16BigEndian(bytes[12..]), BinaryPrimitives.ReadUInt16BigEndian(bytes[14..]));
    }
    public static ScreenPacket? DecodeBgra(ReadOnlySpan<byte> pixels, int cellSize)
    {
        if (cellSize is < 1 or > 16 || pixels.Length != 8 * cellSize * 8 * cellSize * 4) return null;
        Span<byte> bytes = stackalloc byte[24]; bytes.Clear();
        for (var cell = 0; cell < 64; cell++)
        {
            var offset = ((cell / 8 * cellSize + cellSize / 2) * 8 * cellSize + cell % 8 * cellSize + cellSize / 2) * 4;
            for (var channel = 0; channel < 3; channel++)
            {
                var color = pixels[offset + 2 - channel];
                if (color is > 72 and < 183) return null;
                var bit = cell * 3 + channel;
                if (color >= 183) bytes[bit / 8] |= (byte)(1 << (7 - bit % 8));
            }
        }
        return Decode(bytes);
    }
}

// Consumed on one thread. Baseline old packets after start/reload/loss; never replay them.
public sealed class ScreenPacketTracker
{
    private ScreenPacket? previous;
    private uint session;
    private ushort sequence, heartbeat;
    private long advancedAt;
    private bool initialized, heartbeatAdvanced;
    public bool Healthy { get; private set; }
    public void Reset() { previous = null; initialized = heartbeatAdvanced = Healthy = false; }
    public ScreenPacket? Accept(ScreenPacket? packet, long now)
    {
        if (packet is null)
        {
            previous = null;
            if (initialized && now - advancedAt > 1000) Reset();
            return null;
        }
        if (!initialized || session != packet.Session)
        {
            initialized = true; session = packet.Session; sequence = packet.Sequence;
            heartbeat = packet.Heartbeat; advancedAt = now; heartbeatAdvanced = Healthy = false; previous = packet;
            return null;
        }
        if (heartbeat != packet.Heartbeat)
        {
            heartbeat = packet.Heartbeat; advancedAt = now; heartbeatAdvanced = true;
        }
        if (now - advancedAt > 1000) { Reset(); return null; }
        Healthy = heartbeatAdvanced;
        var stable = previous is not null && previous.Session == packet.Session && previous.Sequence == packet.Sequence &&
            previous.EventCode == packet.EventCode && previous.Value == packet.Value && previous.Flags == packet.Flags;
        previous = packet;
        if (!Healthy || !stable || sequence == packet.Sequence) return null;
        var distance = (ushort)(packet.Sequence - sequence);
        if (distance > 32767) return null;
        sequence = packet.Sequence;
        if (packet.EventCode == 0) return null;
        return packet;
    }
}
