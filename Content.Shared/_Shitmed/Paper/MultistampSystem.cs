// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Starlight (Content.Shared/_Starlight/Paper/MultistampSystem.cs) // Inferus
// Inferus - one shared system instead of a shared and a server one, and the stamp list can no longer hold an entity twice
// or point past its end when a stamp comes out.
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._Shitmed.Paper;

public sealed partial class MultistampSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultistampComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MultistampComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<MultistampComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<MultistampComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<MultistampComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnMapInit(Entity<MultistampComponent> ent, ref MapInitEvent args)
    {
        SetMultistamp(ent);
    }

    private void OnHandleState(Entity<MultistampComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        SetMultistamp(ent);
    }

    private void OnInserted(Entity<MultistampComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!HasComp<StampComponent>(args.Entity) || ent.Comp.Stamps.Contains(args.Entity))
            return;

        ent.Comp.Stamps.Add(args.Entity);
        Dirty(ent);
        SetMultistamp(ent);
    }

    private void OnRemoved(Entity<MultistampComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        var index = ent.Comp.Stamps.IndexOf(args.Entity);
        if (index < 0)
            return;

        ent.Comp.Stamps.RemoveAt(index);

        // Keep pointing at the same stamp if one before it left, and never past the end
        if (index < ent.Comp.CurrentEntry)
            ent.Comp.CurrentEntry--;

        if (ent.Comp.CurrentEntry >= ent.Comp.Stamps.Count)
            ent.Comp.CurrentEntry = 0;

        Dirty(ent);
        SetMultistamp(ent);
    }

    private void OnActivate(Entity<MultistampComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (CycleMultistamp(ent, args.User))
            args.Handled = true;
    }

    public bool CycleMultistamp(Entity<MultistampComponent> ent, EntityUid? user = null, bool playSound = true)
    {
        if (ent.Comp.Stamps.Count == 0)
            return false;

        ent.Comp.CurrentEntry = (ent.Comp.CurrentEntry + 1) % ent.Comp.Stamps.Count;
        Dirty(ent);
        SetMultistamp(ent, playSound, user);
        return true;
    }

    /// <summary>
    /// Copies the selected stamp into the StampComponent of the multistamp, so the paper system stamps with it.
    /// </summary>
    public void SetMultistamp(Entity<MultistampComponent> ent, bool playSound = false, EntityUid? user = null)
    {
        if (!TryComp<StampComponent>(ent, out var stamp))
            return;

        var comp = ent.Comp;
        if (comp.CurrentEntry >= comp.Stamps.Count
            || !TryComp<StampComponent>(comp.Stamps[comp.CurrentEntry], out var current))
        {
            comp.CurrentStampName = Loc.GetString("multiple-tool-component-no-behavior");
            Dirty(ent);
            return;
        }

        comp.CurrentStampName = Name(comp.Stamps[comp.CurrentEntry]);
        stamp.StampedName = current.StampedName;
        stamp.StampedColor = current.StampedColor;
        stamp.StampState = current.StampState;
        stamp.Sound = current.Sound;

        if (playSound && comp.ChangeSound != null)
            _audio.PlayPredicted(comp.ChangeSound, ent, user);

        Dirty(ent);
    }
}
