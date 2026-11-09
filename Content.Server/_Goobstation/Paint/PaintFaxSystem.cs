using Content.Shared._Goobstation.Fax;
using Content.Shared._Goobstation.Paint;
using Content.Shared.Popups;

namespace Content.Server._Goobstation.Paint;

public sealed partial class PaintFaxSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaintSurfaceComponent, GettingFaxedCopiedEvent>(OnFaxCopied);
        // Inferus: GettingFaxedSentEvent was not ported with the Goob fax hooks; only block copy for now.
    }

    private void OnFaxCopied(Entity<PaintSurfaceComponent> ent, ref GettingFaxedCopiedEvent args)
    {
        args.Handled = true;

        if (args.Args.Actor.IsValid())
            _popup.PopupEntity(Loc.GetString("paint-fax-use-photocopier"), args.Fax, args.Args.Actor);
    }
}
