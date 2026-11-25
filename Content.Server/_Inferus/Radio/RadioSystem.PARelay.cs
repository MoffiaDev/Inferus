using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared.Radio;
using Robust.Shared.Player;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class RadioSystem
{
    private EntityUid? _paRelaySource;
    private HashSet<ICommonSession>? _paRelayRecipients;

    private void RelayPARadio(EntityUid receiver, ref RadioReceiveEvent ev)
    {
        var previousSource = _paRelaySource;
        var previousRecipients = _paRelayRecipients;
        _paRelaySource = receiver;
        _paRelayRecipients = ev.DirectRecipients;

        try
        {
            RaiseLocalEvent(receiver, ref ev);
        }
        finally
        {
            _paRelaySource = previousSource;
            _paRelayRecipients = previousRecipients;
        }
    }

    private void OnPARelayRecipients(ExpandICChatRecipientsEvent ev)
    {
        if (_paRelaySource != ev.Source || _paRelayRecipients == null)
            return;

        foreach (var session in ev.Recipients.Keys.ToArray())
        {
            if (_paRelayRecipients.Contains(session))
                ev.Recipients[session] = ev.Recipients[session] with { HideChatOverride = true };
        }
    }
}
