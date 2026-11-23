using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Chat.Systems;
using Content.Shared.Eye.Blinking;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Inferus.Eye;

/// <summary>
/// The blink emote should actually close the character's eyelids on the client and play the blink sound.
/// The first blink after eyelids were set up used to "complete" instantly without ever closing the eyes.
/// </summary>
public sealed class BlinkEmoteTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task BlinkEmoteClosesEyelids()
    {
        await RunTicks(10);

        var eyes = EntityUid.Invalid;
        await Client.WaitAssertion(() =>
        {
            var query = CEntMan.EntityQueryEnumerator<EyeBlinkingComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (comp.Body == CPlayer)
                    eyes = uid;
            }
        });

        Assert.That(eyes, Is.Not.EqualTo(EntityUid.Invalid), "Client player should have blinking eyes");

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<ChatSystem>().TryEmoteWithChat(SPlayer, "Blink"), Is.True);
        });

        // Blinks last 0.2-0.5 seconds; watch for the eyelids closing and then reopening.
        var closed = false;
        var reopened = false;
        for (var i = 0; i < 40 && !reopened; i++)
        {
            await RunTicks(1);
            await Client.WaitAssertion(() =>
            {
                var comp = CEntMan.GetComponent<EyeBlinkingComponent>(eyes);
                if (comp.Eyelids.Any(e => e.IsClosed))
                    closed = true;
                else if (closed)
                    reopened = true;
            });
        }

        Assert.Multiple(() =>
        {
            Assert.That(closed, Is.True, "Blink emote should close the eyelids");
            Assert.That(reopened, Is.True, "Eyelids should reopen after the blink");
        });
    }

    [Test]
    public async Task BlinkEmoteSoundExists()
    {
        await Server.WaitAssertion(() =>
        {
            var comp = SEntMan.ComponentFactory.GetComponent<EyeBlinkingComponent>();
            Assert.That(comp.BlinkEmoteSound, Is.TypeOf<SoundCollectionSpecifier>());
            var id = ((SoundCollectionSpecifier) comp.BlinkEmoteSound!).Collection;
            Assert.That(ProtoMan.HasIndex<SoundCollectionPrototype>(id), Is.True, $"Missing sound collection {id}");
        });
    }
}
