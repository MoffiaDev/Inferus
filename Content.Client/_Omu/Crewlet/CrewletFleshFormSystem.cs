// SPDX-License-Identifier: MIT

using Content.Shared._Omu.Crewlet;
using Robust.Client.GameObjects;

namespace Content.Client._Omu.Crewlet;

public sealed partial class CrewletFleshFormSystem : EntitySystem // Inferus
{
    [Dependency] private SpriteSystem _sprite = default!; // Inferus

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrewletFleshFormComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CrewletFleshFormComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<CrewletFleshFormComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.SetVisible((ent, sprite), false);
    }

    private void OnShutdown(Entity<CrewletFleshFormComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent) && TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.SetVisible((ent, sprite), true);
    }
}
