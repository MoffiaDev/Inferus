// SPDX-License-Identifier: MIT

namespace Content.Server._Omu.Crewlet;

[RegisterComponent]
public sealed partial class CrewletOwnerComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Crewlets = new();
}
