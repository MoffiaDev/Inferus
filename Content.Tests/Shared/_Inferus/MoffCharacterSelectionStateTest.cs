using System.Collections.Generic;
using System.Linq;
using Content.Shared._Moffstation.Preferences;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.Tests.Shared._Inferus;

[TestFixture]
public sealed class MoffCharacterSelectionStateTest
{
    [Test]
    public void MissingSlotsRemainEnabledAndMissingJobsRemainNever()
    {
        var state = new MoffCharacterSelectionState();
        Assert.Multiple(() =>
        {
            Assert.That(state.IsSlotEnabled(0), Is.True);
            Assert.That(state.GetPriority("Passenger"), Is.EqualTo(JobPriority.Never));
        });
    }

    [Test]
    public void NormalizeRejectsInvalidPrioritiesAndKeepsOnlyOneHigh()
    {
        var state = new MoffCharacterSelectionState
        {
            JobPriorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>
            {
                ["Passenger"] = JobPriority.High,
                ["StationEngineer"] = JobPriority.High,
                ["Captain"] = JobPriority.Never,
                ["Clown"] = (JobPriority) 99,
                ["Mime"] = (JobPriority) (-1),
            },
        };

        var normalized = state.Normalize();
        Assert.Multiple(() =>
        {
            Assert.That(normalized.JobPriorities.Count, Is.EqualTo(2));
            Assert.That(normalized.JobPriorities.Values.Count(p => p == JobPriority.High), Is.EqualTo(1));
            Assert.That(normalized.JobPriorities.Values.Count(p => p == JobPriority.Medium), Is.EqualTo(1));
            Assert.That(state.GetPriority("StationEngineer"), Is.EqualTo(JobPriority.High));
        });
    }

    [Test]
    public void DeepCopyDoesNotMutateOriginalCollections()
    {
        var original = new MoffCharacterSelectionState
        {
            JobPriorities = new Dictionary<ProtoId<JobPrototype>, JobPriority> { ["Passenger"] = JobPriority.High },
            EnabledSlots = new Dictionary<int, bool> { [1] = false },
        };

        var copy = original.DeepCopy();
        copy.JobPriorities.Clear();
        copy.EnabledSlots[1] = true;
        Assert.Multiple(() =>
        {
            Assert.That(original.GetPriority("Passenger"), Is.EqualTo(JobPriority.High));
            Assert.That(original.IsSlotEnabled(1), Is.False);
        });
    }
}
