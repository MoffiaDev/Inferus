using System.Threading;
using System.Threading.Tasks;
using Content.Server._Moffstation.Preferences;
using Robust.Shared.Player;

namespace Content.Server.Preferences.Managers;

public sealed partial class ServerPreferencesManager
{
    // Seed global priorities only after the default profile exists on a player's first login.
    private async Task LoadDataWithCharacterSelection(ICommonSession session, CancellationToken cancel)
    {
        await LoadData(session, cancel);
        await _dependencies.Resolve<MoffCharacterSelectionManager>().LoadData(session, cancel);
    }
}
