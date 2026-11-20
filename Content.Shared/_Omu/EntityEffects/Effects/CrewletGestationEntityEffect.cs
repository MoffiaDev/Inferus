// SPDX-License-Identifier: MIT

using Content.Shared.FixedPoint; // Inferus - was Content.Goobstation.Maths.FixedPoint
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Omu.EntityEffects.Effects;

public sealed partial class CrewletGestationEntityEffect : EntityEffectBase<CrewletGestationEntityEffect>
{
    [DataField]
    public FixedPoint2 Dose = FixedPoint2.New(10);

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-crewlet-gestation",
            ("chance", Probability),
            ("dose", Dose.Float()));
}
