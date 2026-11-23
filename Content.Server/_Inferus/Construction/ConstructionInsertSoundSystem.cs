using System.Linq;
using Content.Server.Construction.Completions;
using Content.Shared._Inferus.Construction;
using Content.Shared.Construction;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Inferus.Construction;

/// <summary>
/// Plays a "place" sound (metal, glass, wires, electronics or a generic item) whenever an item is put into a
/// construction graph step, a machine frame, or consumed by a crafting/construction menu recipe.
/// Which sound plays is decided by <see cref="ConstructionInsertSoundPrototype"/> rules.
/// </summary>
public sealed class ConstructionInsertSoundSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private List<ConstructionInsertSoundPrototype> _rules = new();
    private ConstructionInsertSoundPrototype? _fallback;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        CacheRules();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ConstructionInsertSoundPrototype>())
            CacheRules();
    }

    private void CacheRules()
    {
        _rules = _proto.EnumeratePrototypes<ConstructionInsertSoundPrototype>()
            .Where(p => !p.Fallback)
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.ID)
            .ToList();

        _fallback = _proto.EnumeratePrototypes<ConstructionInsertSoundPrototype>()
            .Where(p => p.Fallback)
            .OrderByDescending(p => p.Priority)
            .FirstOrDefault();
    }

    /// <summary>
    /// Picks the place-sound rule for an item that is about to be inserted or consumed.
    /// Call this before the item is deleted.
    /// </summary>
    public ConstructionInsertSoundPrototype? GetRule(EntityUid item)
    {
        if (TerminatingOrDeleted(item))
            return _fallback;

        TryComp<StackComponent>(item, out var stack);
        TryComp<TagComponent>(item, out var tags);

        foreach (var rule in _rules)
        {
            if (Matches(rule, item, stack, tags))
                return rule;
        }

        return _fallback;
    }

    private bool Matches(ConstructionInsertSoundPrototype rule, EntityUid item, StackComponent? stack, TagComponent? tags)
    {
        if (stack != null && rule.Stacks.Contains(stack.StackTypeId))
            return true;

        if (tags != null)
        {
            foreach (var tag in tags.Tags)
            {
                if (rule.Tags.Contains(tag))
                    return true;

                foreach (var pattern in rule.TagPatterns)
                {
                    if (tag.Id.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
        }

        if (rule.Components.Count == 0 && rule.ComponentPatterns.Count == 0)
            return false;

        foreach (var comp in EntityManager.GetComponents(item))
        {
            var name = Factory.GetComponentName(comp.GetType());

            if (rule.Components.Contains(name))
                return true;

            foreach (var pattern in rule.ComponentPatterns)
            {
                if (name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether any of the given graph actions already play a sound, in which case the automatic
    /// place sound is skipped to avoid playing two sounds at once.
    /// </summary>
    public static bool HasSoundAction(IEnumerable<IGraphAction>? actions)
    {
        return actions != null && actions.Any(a => a is PlaySound);
    }

    /// <summary>
    /// Plays the given rule's sound at the given coordinates.
    /// </summary>
    public void Play(ConstructionInsertSoundPrototype? rule, EntityCoordinates coordinates)
    {
        if (rule == null)
            return;

        var scale = (float) _random.NextGaussian(1, rule.Variation);
        _audio.PlayPvs(rule.Sound, coordinates, AudioParams.Default.WithPitchScale(scale));
    }

    /// <summary>
    /// Plays the given rule's sound on the given entity, if it still exists.
    /// </summary>
    public void Play(ConstructionInsertSoundPrototype? rule, EntityUid target)
    {
        if (rule == null || TerminatingOrDeleted(target))
            return;

        Play(rule, Transform(target).Coordinates);
    }

    /// <summary>
    /// Convenience method: picks a rule for <paramref name="item"/> and plays it on <paramref name="target"/>.
    /// </summary>
    public void PlayFor(EntityUid item, EntityUid target)
    {
        Play(GetRule(item), target);
    }
}
