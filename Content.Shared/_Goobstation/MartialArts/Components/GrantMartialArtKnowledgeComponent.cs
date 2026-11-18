// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.MartialArts;
using Content.Shared.NPC.Prototypes;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Goobstation.MartialArts.Components;

public abstract partial class GrantMartialArtKnowledgeComponent : Component
{
    [DataField]
    public virtual MartialArtsForms MartialArtsForm { get; set; } = MartialArtsForms.CloseQuartersCombat;

    [DataField]
    public virtual LocId? LearnMessage { get; set; } = null;

    [DataField]
    public bool MultiUse;

    [DataField]
    public string? SpawnedProto = "Ash";

    [DataField]
    public SoundSpecifier? SoundOnUse = new SoundPathSpecifier("/Audio/Effects/fire.ogg", AudioParams.Default.WithVolume(10));
}

[RegisterComponent]
public sealed partial class GrantCqcComponent : GrantMartialArtKnowledgeComponent
{
    [DataField]
    public bool IsBlocked;

    public override LocId? LearnMessage { get; set; } = "cqc-success-learned";
}

[RegisterComponent]
public sealed partial class GrantCorporateJudoComponent : GrantMartialArtKnowledgeComponent
{
    [DataField]
    public override MartialArtsForms MartialArtsForm { get; set; } = MartialArtsForms.CorporateJudo;
}

[RegisterComponent]
public sealed partial class GrantCapoeiraComponent : GrantMartialArtKnowledgeComponent
{
    [DataField]
    public override MartialArtsForms MartialArtsForm { get; set; } = MartialArtsForms.Capoeira;

    public override LocId? LearnMessage { get; set; } = "capoeira-success-learned";
}

[RegisterComponent]
public sealed partial class GrantKungFuDragonComponent : GrantMartialArtKnowledgeComponent
{
    [DataField]
    public override MartialArtsForms MartialArtsForm { get; set; } = MartialArtsForms.KungFuDragon;

    public override LocId? LearnMessage { get; set; } = "dragon-success-learned";
}

[RegisterComponent]
public sealed partial class GrantNinjutsuComponent : GrantMartialArtKnowledgeComponent
{
    [DataField]
    public override MartialArtsForms MartialArtsForm { get; set; } = MartialArtsForms.Ninjutsu;

    public override LocId? LearnMessage { get; set; } = "ninjutsu-success-learned";
}

[RegisterComponent]
public sealed partial class GrantSleepingCarpComponent : GrantMartialArtKnowledgeComponent
{
    [DataField]
    public override MartialArtsForms MartialArtsForm { get; set; } = MartialArtsForms.SleepingCarp;

    [DataField]
    public int MaximumUses = 1;
    public int CurrentUses = 0;

    // Inferus - removed, Inferus has no Sleeping Carp faction icon
    // [DataField]
    // public ProtoId<FactionIconPrototype> IconToAdd = "SleepingCarpFaction";
    // End Inferus

    [DataField]
    public ProtoId<NpcFactionPrototype> FactionToAdd = "Dragon";
}

[RegisterComponent]
public sealed partial class SleepingCarpStudentComponent : Component
{
    [DataField]
    public int Stage = 1;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan UseAgainTime = TimeSpan.Zero;

    [DataField]
    public int MaxUseDelay = 90;

    [DataField]
    public int MinUseDelay = 30;
}

[RegisterComponent]
public sealed partial class GrantHellRipComponent : GrantMartialArtKnowledgeComponent
{
    [DataField]
    public override MartialArtsForms MartialArtsForm { get; set; } = MartialArtsForms.HellRip;

    public override LocId? LearnMessage { get; set; } = "hellrip-success-learned";
}
