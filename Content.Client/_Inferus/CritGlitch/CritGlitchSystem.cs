using Content.Shared._Inferus.CCVar;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Inferus.CritGlitch;

/// <summary>
/// Inferus
/// While the local player is in crit, applies a glitch/censor shader to the
/// sprites of other characters in view
/// </summary>
public sealed partial class CritGlitchSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    private static ProtoId<ShaderPrototype> ShaderId = "InferusCritGlitch";

    private const float RefreshInterval = 1.0f;

    private ShaderInstance _shader = default!;
    private HashSet<EntityUid> _glitched = new();

    private bool _enabled; // local player is currently in crit
    private bool _allowed = true; // CVar
    private float _critLevel = 0.5f;
    private float _refreshTimer;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MobThresholdChecked>(OnThresholdChecked);
        // Humanoids entering PVS while we are in crit get picked up here
        SubscribeLocalEvent<HumanoidProfileComponent, ComponentStartup>(OnHumanoidStartup);

        _cfg.OnValueChanged(InferusCCVars.CritGlitchOthers, OnCVarChanged, invokeImmediately: true);
        _shader = _proto.Index(ShaderId).InstanceUnique();
        _shader.SetParameter("critLevel", _critLevel);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled)
            return;

        // Periodic refresh catches entities that lost their shader to another effect ending (e.g. a cloak dropping) while we were in crit
        _refreshTimer += frameTime;
        if (_refreshTimer < RefreshInterval)
            return;

        _refreshTimer = 0f;
        ApplyToVisible();
    }

    private void OnCVarChanged(bool value)
    {
        _allowed = value;

        if (!_allowed)
            SetActive(false);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        // Clean slate in case we were in crit when detached (ghosting etc.)
        SetActive(false);

        if (TryComp<MobStateComponent>(args.Entity, out var mobState) &&
            mobState.CurrentState is MobState.Critical or MobState.SoftCritical or MobState.HardCritical)
        {
            SetActive(true);
            UpdateCritLevel();
        }
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        SetActive(false);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.Target != _player.LocalEntity)
            return;

        if (args.NewMobState is not (MobState.Critical or MobState.SoftCritical or MobState.HardCritical))
        {
            SetActive(false);
            return;
        }

        SetActive(true);
        UpdateCritLevel();
    }

    private void OnThresholdChecked(ref MobThresholdChecked args)
    {
        if (args.Target != _player.LocalEntity || !_enabled)
            return;

        // Reuse the same depth value the crit distortion overlay uses so the glitch on other characters intensifies the worse into crit you go.
        // TotalDamage is access-restricted to DamageableSystem, hence the accessor
#pragma warning disable CS0618 // SSSHUUUUUT UUUUUUUUUUUP!!!!!!!
        var totalDamage = _damageable.GetTotalDamage((args.Target, args.Damageable));
#pragma warning restore CS0618
        if (_mobThreshold.TryGetDeadPercentage(args.Target, totalDamage, out var percentage))
        {
            _critLevel = Math.Clamp(percentage.Value.Float(), 0f, 1f);
            _shader.SetParameter("critLevel", _critLevel);
        }
    }

    private void OnHumanoidStartup(Entity<HumanoidProfileComponent> entity, ref ComponentStartup args)
    {
        if (_enabled)
            TryApply(entity);
    }

    private void SetActive(bool active)
    {
        if (active == _enabled)
            return;

        _enabled = active;
        _refreshTimer = 0f;

        if (active)
        {
            _critLevel = 0.5f;
            _shader.SetParameter("critLevel", _critLevel);
            ApplyToVisible();
        }
        else
        {
            ClearAll();
        }
    }

    private void UpdateCritLevel()
    {
        var uid = _player.LocalEntity;
        if (uid == null || !TryComp<DamageableComponent>(uid, out var damageable))
            return;

#pragma warning disable CS0618 // same obsolete accessor the crit damage overlay uses
        var totalDamage = _damageable.GetTotalDamage((uid.Value, damageable));
#pragma warning restore CS0618
        if (_mobThreshold.TryGetDeadPercentage(uid.Value, totalDamage, out var percentage))
        {
            _critLevel = Math.Clamp(percentage.Value.Float(), 0f, 1f);
            _shader.SetParameter("critLevel", _critLevel);
        }
    }

    private void ApplyToVisible()
    {
        var query = EntityQueryEnumerator<HumanoidProfileComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            TryApply(uid);
        }
    }

    private void TryApply(EntityUid uid)
    {
        if (uid == _player.LocalEntity)
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (sprite.PostShader == _shader)
        {
            _glitched.Add(uid);
            return;
        }

        if (sprite.PostShader != null)
        {
            _glitched.Remove(uid);
            return;
        }

        // Free slot – apply.
        sprite.PostShader = _shader;
        _glitched.Add(uid);
    }

    private void ClearAll()
    {
        // Full scan so we never leave the shader stuck on an entity that
        // is no longer in the tracking set (PVS leave, deletion, set desync, …).
        var query = EntityQueryEnumerator<HumanoidProfileComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            if (sprite.PostShader == _shader)
                sprite.PostShader = null;
        }

        _glitched.Clear();
    }
}
