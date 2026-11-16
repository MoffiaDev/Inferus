// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.MartialArts;
using Content.Shared._Goobstation.MartialArts.Components;
using Content.Shared._Goobstation.MartialArts.Events;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Polymorph;

namespace Content.Server._Goobstation.MartialArts;

/// <summary>
/// Just handles carp sayings for now.
/// </summary>
public sealed partial class MartialArtsSystem : SharedMartialArtsSystem
{
    [Dependency] private ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CanPerformComboComponent, SleepingCarpSaying>(OnSleepingCarpSaying);
        SubscribeLocalEvent<CanPerformComboComponent, PolymorphedEvent>(OnPolymorphedCPC);
        SubscribeLocalEvent<MartialArtsKnowledgeComponent, PolymorphedEvent>(OnPolymorphedMAK);
    }

    private void OnPolymorphedCPC(Entity<CanPerformComboComponent> ent, ref PolymorphedEvent args)
    {
        if (HasComp<CanPerformComboComponent>(args.NewEntity))
            return;

        CopyComp(ent.Owner, args.NewEntity, ent.Comp); // Inferus - Goob's PolymorphSystem.CopyPolymorphComponent is not ported
    }

    private void OnPolymorphedMAK(Entity<MartialArtsKnowledgeComponent> ent, ref PolymorphedEvent args)
    {
        if (HasComp<MartialArtsKnowledgeComponent>(args.NewEntity))
            return;

        CopyComp(ent.Owner, args.NewEntity, ent.Comp); // Inferus - Goob's PolymorphSystem.CopyPolymorphComponent is not ported
    }

    private void OnSleepingCarpSaying(Entity<CanPerformComboComponent> ent, ref SleepingCarpSaying args)
    {
        _chat.TrySendInGameICMessage(ent, Loc.GetString(args.Saying), InGameICChatType.Speak, false);
    }
}
