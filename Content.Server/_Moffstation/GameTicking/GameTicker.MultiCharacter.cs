using Content.Server.Players.JobWhitelist;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [Dependency] private JobWhitelistManager _moffJobWhitelist = default!;
}


