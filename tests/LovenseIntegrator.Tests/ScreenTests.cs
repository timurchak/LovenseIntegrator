using System.Text;
using LovenseIntegrator.Core;
using LovenseIntegrator.Desktop.Services;

internal static class ScreenTests
{
    public static void Run(Action<bool, string> check)
    {
        var packet = new ScreenPacket(0x12345678, 65535, 3, 2, 42, 321);
        var encoded = ScreenProtocol.Encode(packet);
        check(Convert.ToHexString(encoded) == "5753450112345678FFFF0302002A0141000000000000290C", "Screen C#/Lua independent golden vector");
        check(ScreenProtocol.Crc(Encoding.ASCII.GetBytes("123456789")) == 0x29b1, "Screen CRC standard check value");
        check(ScreenProtocol.Decode(encoded) == packet, "Screen packet roundtrip");
        for (var bit = 0; bit < encoded.Length * 8; bit++)
        {
            var corrupt = encoded.ToArray(); corrupt[bit / 8] ^= (byte)(1 << bit % 8);
            if (ScreenProtocol.Decode(corrupt) is not null) throw new Exception("Accepted corrupted Screen bit " + bit);
        }
        check(true, "Screen rejects every single-bit corruption");
        check(ScreenProtocol.Decode(ScreenProtocol.Encode(packet with { EventCode = 200 })) is null && ScreenProtocol.Decode(ScreenProtocol.Encode(packet with { Flags = 4 })) is null, "Screen rejects unknown events and flags even with valid CRC");
        check(ScreenProtocol.Decode(encoded.AsSpan(1)) is null && ScreenProtocol.DecodeBgra([], 0) is null, "Screen rejects malformed buffer and geometry");
        foreach (var size in new[] { 1, 2, 4, 8, 16 })
        {
            var image = Pixels(encoded, size);
            check(ScreenProtocol.DecodeBgra(image, size) == packet, $"Screen RGB bit order, BGRA stride and scale {size}");
            var center = ((size / 2) * size * 8 + size / 2) * 4;
            image[center] = 128;
            check(ScreenProtocol.DecodeBgra(image, size) is null, $"Screen rejects ambiguous center color at scale {size}");
        }
        var tracker = new ScreenPacketTracker();
        check(tracker.Accept(packet, 0) is null && !tracker.Healthy, "Screen startup baselines old events");
        check(tracker.Accept(packet, 50) is null && !tracker.Healthy, "Screen frozen initial packet cannot establish liveness");
        var idle = packet with { Heartbeat = 322, EventCode = 0 };
        tracker.Accept(idle, 100);
        check(tracker.Healthy, "Screen advancing heartbeat establishes liveness");
        var next = packet with { Sequence = 0, Heartbeat = 323 };
        check(tracker.Accept(next, 150) is null && tracker.Accept(next, 200) == next, "Screen sequence rollover and two-sample debounce");
        check(tracker.Accept(next with { Heartbeat = 324 }, 250) is null, "Screen heartbeat is not another event");
        var old = next with { Sequence = 65535, Heartbeat = 325 };
        tracker.Accept(old, 300); tracker.Accept(old, 350);
        tracker.Accept(next, 400);
        check(tracker.Accept(next, 450) is null, "Screen out-of-order frames cannot rewind deduplication and replay an event");
        tracker.Accept(null, 1500);
        check(!tracker.Healthy && tracker.Accept(next, 1550) is null, "Screen missing signal expires and recovery baselines");
        tracker.Accept(next with { Heartbeat = 325 }, 1600);
        tracker.Accept(next with { Heartbeat = 325 }, 2700);
        check(!tracker.Healthy, "Screen valid but frozen signal expires");
        tracker.Reset(); tracker.Accept(packet, 0); tracker.Accept(packet with { Heartbeat = 322 }, 100);
        check(tracker.Accept(packet with { Session = 44, Sequence = 10 }, 150) is null && !tracker.Healthy, "Screen addon reload invalidates liveness and events");
        var engine = new RuleEngine(); engine.Reset(0);
        var rule = new Rule { Event = EventKind.ScreenEvent, ScreenEventId = "wow.player.damage", Process = "Wow", CooldownMs = 300 };
        InputEvent Damage(long at, string id = "wow.player.damage") => new(EventKind.ScreenEvent, at, Process: "Wow", ScreenEventId: id);
        check(engine.Process(Damage(0), [rule])?.Rule.Id == rule.Id, "Screen semantic event routes by dedicated ID");
        check(engine.Process(Damage(100), [rule]) is null && engine.Process(Damage(300, "wow.player.heal"), [rule]) is null, "Screen cooldown and distinct event filtering");
        check(engine.Process(Damage(301), [rule]) is not null, "Screen cooldown expires");
        check(engine.Process(Damage(700) with { Process = "other" }, [rule]) is null, "Screen process filter enforced");
        var keyboard = new Rule { Event = EventKind.KeyHeld, Keys = "A", Threshold = 100, CooldownMs = 100 };
        engine.Reset(0); engine.Process(new(EventKind.KeyDown, 0, "A"), [keyboard]);
        engine.Process(new(EventKind.Timer, 100), [keyboard]);
        engine.Process(Damage(200), [keyboard]);
        check(engine.Process(new(EventKind.Timer, 300), [keyboard]) is null, "Screen telemetry cannot reset keyboard hold latch");
        var invalid = rule.Copy(); invalid.ScreenEventId = "wow.test";
        check(invalid.Validate() is not null, "Screen preview-only test cannot be assigned");
        invalid.ScreenEventId = "unknown"; check(invalid.Validate() is not null, "Screen unknown semantic ID rejected");
        invalid = rule.Copy(); invalid.Action = ActionKind.RateMapped;
        check(invalid.Validate() is not null, "Screen disallows typing-speed action");
        var preset = ScreenPresetStore.Parse(File.ReadAllText("examples/screen-assignments.json"));
        var json = File.ReadAllText("examples/screen-assignments.json");
        foreach (var bad in new[] { json.Replace("\"Version\": 1", "\"Version\": 2"), json.Replace("\"Enabled\": true", "\"Enabled\": true, \"Enabled\": false"), json.Replace("wow.player.damage", "wow.test"), json.Replace("\"ScreenEventId\"", "\"UnknownField\"") })
        {
            var rejected = false;
            try { ScreenPresetStore.Parse(bad); } catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidDataException) { rejected = true; }
            check(rejected, "Screen strict import rejects invalid contract");
        }
        var merged = ScreenPresetStore.Merge([keyboard], preset.Rules);
        check(ScreenPresetStore.Merge(merged, preset.Rules).Count == 2 && merged[0].Id == keyboard.Id, "Screen merge is idempotent and preserves other modes");
        var collision = preset.Rules[0].Copy(); collision.Id = keyboard.Id;
        var collisionRejected = false;
        try { ScreenPresetStore.Merge([keyboard], [collision]); } catch (InvalidDataException) { collisionRejected = true; }
        check(collisionRejected, "Screen import cannot replace other mode by ID");
        var directory = Path.GetFullPath("artifacts/screen-tests"); Directory.CreateDirectory(directory);
        var profilePath = Path.Combine(directory, "profile.json");
        ProfileStore.Save(new Profile { Rules = merged }, profilePath);
        check(ProfileStore.Load(profilePath).Rules[1].ScreenEventId == preset.Rules[0].ScreenEventId, "Profile v3 roundtrips Screen assignment");
    }
    internal static byte[] Pixels(byte[] bytes, int size)
    {
        var side = 8 * size; var pixels = new byte[side * side * 4];
        for (var y = 0; y < side; y++) for (var x = 0; x < side; x++) for (var c = 0; c < 3; c++)
        {
            var bit = (y / size * 8 + x / size) * 3 + c;
            pixels[(y * side + x) * 4 + 2 - c] = (bytes[bit / 8] & (1 << (7 - bit % 8))) != 0 ? (byte)255 : (byte)0;
        }
        return pixels;
    }
}
