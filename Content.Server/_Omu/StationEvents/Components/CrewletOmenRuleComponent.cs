// SPDX-License-Identifier: MIT

using Content.Server._Omu.StationEvents.Events;

namespace Content.Server._Omu.StationEvents.Components;

[RegisterComponent, Access(typeof(CrewletOmenRule))]
public sealed partial class CrewletOmenRuleComponent : Component;
