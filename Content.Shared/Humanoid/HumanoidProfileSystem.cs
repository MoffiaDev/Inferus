using System.Numerics; // Goobstation - port EE height/width sliders
using Content.Shared._EinsteinEngines.HeightAdjust; // Goobstation - port EE height/width sliders
using Content.Shared._Inferus.Speech; // Inferus
using Content.Shared.Examine;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidProfileSystem : EntitySystem
{
    [Dependency] private GrammarSystem _grammar = default!;
    [Dependency] private SpeechBlipSystem _speechBlip = default!; // Inferus
    [Dependency] private HeightAdjustSystem _heightAdjust = default!; // Goobstation - port EE height/width sliders

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidProfileComponent, ExaminedEvent>(OnExamined);
    }

    public void ApplyProfileTo(Entity<HumanoidProfileComponent?> ent, HumanoidCharacterProfile profile)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Gender = profile.Gender;
        ent.Comp.Age = profile.Age;
        ent.Comp.Species = profile.Species;
        ent.Comp.CustomSpecieName = profile.CustomSpecieName; // Inferus: Starlight custom species name port
        ent.Comp.Voice = profile.Voice;
        ent.Comp.Sex = profile.Sex;

        // Goobstation - Start - port EE height/width sliders
        var oldScale = new Vector2(ent.Comp.Width, ent.Comp.Height);
        var newScale = ClampScale(profile.Species, new Vector2(profile.Width, profile.Height));
        ent.Comp.Width = newScale.X;
        ent.Comp.Height = newScale.Y;
        // Goobstation - End - port EE height/width sliders

        Dirty(ent);

        _heightAdjust.SetScale(ent.Owner, newScale, oldScale); // Goobstation - port EE height/width sliders

        var voiceChanged = new VoiceChangedEvent(ent.Comp.Voice, profile.Voice);
        RaiseLocalEvent(ent, ref voiceChanged);

        if (TryComp<GrammarComponent>(ent, out var grammar))
        {
            _grammar.SetGender((ent, grammar), profile.Gender);
        }

        _speechBlip.SetBlip(ent, profile.SpeechBlip); // Inferus
        _speechBlip.SetPitch(ent, profile.SpeechBlipPitch); // Inferus
    }

    // Goobstation - Start - port EE height/width sliders
    /// <summary>
    ///     Clamps a width (X) / height (Y) scale to the limits of the given species.
    ///     Non-positive values are replaced with the species defaults.
    /// </summary>
    public Vector2 ClampScale(ProtoId<SpeciesPrototype> species, Vector2 scale)
    {
        if (!ProtoMan.TryIndex(species, out var proto))
            return scale.X <= 0 || scale.Y <= 0 ? Vector2.One : scale;

        if (scale.X <= 0 || scale.Y <= 0)
            return new Vector2(proto.DefaultWidth, proto.DefaultHeight);

        return new Vector2(
            Math.Clamp(scale.X, proto.MinWidth, proto.MaxWidth),
            Math.Clamp(scale.Y, proto.MinHeight, proto.MaxHeight));
    }

    /// <summary>
    ///     Sets the height and width of a humanoid, clamped to its species limits.
    /// </summary>
    public void SetScale(Entity<HumanoidProfileComponent?> ent, Vector2 scale)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var oldScale = new Vector2(ent.Comp.Width, ent.Comp.Height);
        var newScale = ClampScale(ent.Comp.Species, scale);
        ent.Comp.Width = newScale.X;
        ent.Comp.Height = newScale.Y;
        Dirty(ent);

        _heightAdjust.SetScale(ent.Owner, newScale, oldScale);
    }
    // Goobstation - End - port EE height/width sliders

    private void OnExamined(Entity<HumanoidProfileComponent> ent, ref ExaminedEvent args)
    {
        var identity = Identity.Entity(ent, EntityManager);
        var species = GetSpeciesRepresentation(ent.Comp.Species, ent.Comp.CustomSpecieName).ToLower(); // Inferus: was GetSpeciesRepresentation(ent.Comp.Species)
        var age = GetAgeRepresentation(ent.Comp.Species, ent.Comp.Age);

        args.PushText(Loc.GetString("humanoid-appearance-component-examine", ("user", identity), ("age", age), ("species", species)));
    }

    /// <summary>
    /// Takes ID of the species prototype, returns UI-friendly name of the species.
    /// </summary>
    public string GetSpeciesRepresentation(ProtoId<SpeciesPrototype> species)
    {
        if (ProtoMan.TryIndex(species, out var speciesPrototype))
            return Loc.GetString(speciesPrototype.Name);

        Log.Error("Tried to get representation of unknown species: {speciesId}");
        return Loc.GetString("humanoid-appearance-component-unknown-species");
    }

    // Inferus - Starlight custom species name port
    /// <summary>
    /// Returns the custom species name if one is set, otherwise the UI-friendly name of the species.
    /// </summary>
    public string GetSpeciesRepresentation(ProtoId<SpeciesPrototype> species, string? customSpecieName)
    {
        return string.IsNullOrWhiteSpace(customSpecieName)
            ? GetSpeciesRepresentation(species)
            : customSpecieName;
    }
    // End Inferus

    /// <summary>
    /// Takes ID of the species prototype and an age, returns an approximate description
    /// </summary>
    public string GetAgeRepresentation(ProtoId<SpeciesPrototype> species, int age)
    {
        if (!ProtoMan.TryIndex(species, out var speciesPrototype))
        {
            Log.Error("Tried to get age representation of species that couldn't be indexed: " + species);
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.YoungAge)
        {
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.OldAge)
        {
            return Loc.GetString("identity-age-middle-aged");
        }

        return Loc.GetString("identity-age-old");
    }
}
