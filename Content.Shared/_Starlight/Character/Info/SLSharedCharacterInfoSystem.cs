// SPDX-FileCopyrightText: 2025 Starlight
// SPDX-License-Identifier: Starlight-MIT

using Content.Shared._Starlight.Character.Info.Components;
using Content.Shared._Starlight.CCVar;
using Content.Shared.CCVar;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Players;
using Content.Shared.Preferences;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._Starlight.Character.Info;

/// <summary>
/// Handles reading/writing character info (custom physical/personality descriptions, OOC notes and personal notes).
/// </summary>
public abstract partial class SLSharedCharacterInfoSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examineSystem = default!;
    [Dependency] private SharedMindSystem _mindSystem = default!;
    [Dependency] private IConfigurationManager _configManager = default!;

    private static readonly SpriteSpecifier InspectIcon =
        new SpriteSpecifier.Texture(new("/Textures/_Starlight/Interface/VerbIcons/examine-character-menu.png"));

    private static readonly SpriteSpecifier DetailIcon =
        new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/examine.svg.192dpi.png"));

    private bool _characterWindowEnabled;
    private bool _flavorTextEnabled;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configManager, CCVars.FlavorText, b => _flavorTextEnabled = b, true);
        Subs.CVar(_configManager, StarlightCCVars.CharacterInspectWindowEnabled, b => _characterWindowEnabled = b, true);

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<ActivateImplantEvent>(OnActivateImplant);

        SubscribeLocalEvent<HumanoidProfileComponent, GetVerbsEvent<ExamineVerb>>(OnExamineCharacter); // Inferus: was HumanoidAppearanceComponent
        SubscribeLocalEvent<MindSecretsComponent, ComponentGetStateAttemptEvent>(AttemptSyncMindSecrets);
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        ApplyCharacterInfo(ev.Mob, ev.Profile);
    }

    /// <summary>
    /// Applies the profile's physical description to the mob, and the personality description,
    /// OOC notes and personal notes to the mob's mind if it has one.
    /// Safe to call more than once; existing components are replaced.
    /// </summary>
    public void ApplyCharacterInfo(EntityUid mob, HumanoidCharacterProfile profile)
    {
        var newMind = _mindSystem.GetMind(mob);
        var applyFlavorText = !HasComp<RemoveFlavorTextComponent>(mob);
        if (newMind != null && HasComp<MindComponent>(newMind))
        {
            if (applyFlavorText && _configManager.GetCVar(CCVars.FlavorText))
            {
                var personalityDescription = new CharacterDescriptionComponent
                {
                    Description = profile.PersonalityDescription,
                };
                AddComp(newMind.Value, personalityDescription, true);
            }

            if (_configManager.GetCVar(StarlightCCVars.OOCNotes))
            {
                var roleplayInfo = new RoleplayInfoComponent { OOCNotes = profile.OOCNotes };
                AddComp(newMind.Value, roleplayInfo, true);

                if (applyFlavorText)
                {
                    var mindSecrets = new MindSecretsComponent { PersonalNotes = profile.PersonalNotes };
                    AddComp(newMind.Value, mindSecrets, true);
                }
            }
        }

        if (!applyFlavorText)
            return;

        if (_configManager.GetCVar(CCVars.FlavorText))
        {
            var charDescription = new CharacterDescriptionComponent { Description = profile.PhysicalDescription };
            AddComp(mob, charDescription, true);
        }
    }

    private void OnActivateImplant(ActivateImplantEvent ev)
    {
        if (!HasComp<DnaScrambleOnTriggerComponent>(ev.Action.Comp.Container))
            return;

        if (TryComp(ev.Performer, out CharacterDescriptionComponent? characterDescription))
        {
            characterDescription.Description = string.Empty;
            Dirty(ev.Performer, characterDescription);
        }

        // Also scramble the personality/mind flavortext.
        if (!TryComp(ev.Performer, out MindContainerComponent? mindCont)
            || !mindCont.HasMind
            || !TryComp(mindCont.Mind, out CharacterDescriptionComponent? personalityDescription))
            return;

        personalityDescription.Description = string.Empty;
        Dirty(mindCont.Mind.Value, personalityDescription);
    }

    private void AttemptSyncMindSecrets(EntityUid uid, MindSecretsComponent component,
        ref ComponentGetStateAttemptEvent args)
    {
        var playerMind = args.Player?.GetMind();
        if (playerMind == null)
            return;
        args.Cancelled = playerMind != uid;
    }

    private void OnExamineCharacter(Entity<HumanoidProfileComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
            return;
        var user = args.User;

        var detailsRange = _examineSystem.IsInDetailsRange(args.User, ent);

        if (_characterWindowEnabled)
        {
            args.Verbs.Add(new ExamineVerb
            {
                Act = () =>
                {
                    OpenCharacterWindow(ent, user);
                },
                Disabled = !detailsRange,
                Message = detailsRange ? null : Loc.GetString("detail-examine-verb-disabled"),
                Text = Loc.GetString("character-info-inspect-prompt"),
                Icon = InspectIcon,
            });
        }

        var mind = _mindSystem.GetMind(ent);
        string? desc = null;
        if (TryComp(args.Target, out CharacterDescriptionComponent? characterDesc) &&
            characterDesc.Description.Length > 0)
        {
            desc += characterDesc.Description;
        }

        if (mind != null && TryComp(mind, out CharacterDescriptionComponent? mindInfoComponent) &&
            mindInfoComponent.Description.Length > 0)
        {
            desc = desc == null ? mindInfoComponent.Description : desc + "\n" + mindInfoComponent.Description; // Inferus: no leading newline without a physical description
        }

        if (!_flavorTextEnabled || desc == null)
            return;

        args.Verbs.Add(new ExamineVerb
        {
            Act = () =>
            {
                var markup = new FormattedMessage();
                markup.AddMarkupPermissive(desc);
                _examineSystem.SendExamineTooltip(user, ent, markup, false, false);
            },
            Text = Loc.GetString("detail-examine-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString("detail-examine-verb-disabled"),
            Icon = DetailIcon,
        });
    }

    protected virtual void OpenCharacterWindow(EntityUid target, EntityUid requester)
    {
    }

    public bool CanAccessSecretData(EntityUid target, Entity<MindContainerComponent?> requester)
    {
        return target == requester.Owner;
    }
}
