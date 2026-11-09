using Content.Shared.Fax;
using Content.Shared.Fax.Components;

namespace Content.Shared._Goobstation.Fax;

[ByRefEvent]
public record struct GettingFaxedCopiedEvent(Entity<FaxMachineComponent> Fax, FaxCopyMessage Args, bool Handled = false);
