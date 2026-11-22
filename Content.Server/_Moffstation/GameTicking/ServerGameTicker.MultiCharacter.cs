using Content.Server.Players.JobWhitelist;

namespace Content.Server.GameTicking;

public sealed partial class ServerGameTicker
{
    [Dependency] private JobWhitelistManager _moffJobWhitelist = default!;
}


