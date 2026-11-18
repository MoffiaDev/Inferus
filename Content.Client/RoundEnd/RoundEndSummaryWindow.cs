using System.Linq;
using System.Numerics;
using Content.Client.Message;
using Content.Client.Stylesheets; // Goob Station - End of Round Screen
using Content.Shared.Mobs; // Goob Station - End of Round Screen
using RoundEndPlayerInfo = Content.Shared.GameTicking.RoundEndMessageEvent.RoundEndPlayerInfo;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.RoundEnd;

/// <summary>
/// Window displaying round end information including player manifest.
/// </summary>
public sealed partial class RoundEndSummaryWindow : DefaultWindow
{
    [Dependency] private IEntityManager _entityManager = default!;

    public int RoundId;
    private readonly RoundEndPlayerInfo[] _playersInfo;
    private GridContainer _playerGrid = null!;
    private readonly List<SortButton> _sortButtons = [];
    private string _searchText = string.Empty;

    private enum SortField
    {
        ICName,
        Role,
        PlayerType,
        OOCName
    }

    private SortField _currentSortField = SortField.PlayerType;
    private bool _sortDescending;

    public RoundEndSummaryWindow(string gm, string roundEnd, TimeSpan roundTimeSpan, int roundId, RoundEndPlayerInfo[] info)
    {
        IoCManager.InjectDependencies(this);
        _playersInfo = info;

        MinSize = SetSize = new Vector2(720, 580);

        Title = Loc.GetString("round-end-summary-window-title");

        // The round end window is split into two tabs, one about the round stats
        // and the other is a list of RoundEndPlayerInfo for each player.
        // This tab would be a good place for things like: "x many people died.",
        // "clown slipped the crew x times.", "x shots were fired this round.", etc.
        // Also, good for serious info.

        RoundId = roundId;
        var roundEndTabs = new TabContainer();
        roundEndTabs.AddChild(MakeRoundEndSummaryTab(gm, roundEnd, roundTimeSpan, roundId));
        roundEndTabs.AddChild(MakePlayerManifestTab());

        ContentsContainer.AddChild(roundEndTabs);

        OpenCenteredRight();
        MoveToFront();
    }

    private static BoxContainer MakeRoundEndSummaryTab(string gamemode, string roundEnd, TimeSpan roundDuration, int roundId)
    {
        var roundEndSummaryTab = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Name = Loc.GetString("round-end-summary-window-round-end-summary-tab-title")
        };

        var roundEndSummaryContainerScrollbox = new ScrollContainer
        {
            VerticalExpand = true,
            Margin = new Thickness(10)
        };
        var roundEndSummaryContainer = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical
        };

        //Gamemode Name
        var gamemodeLabel = new RichTextLabel();
        var gamemodeMessage = new FormattedMessage();
        gamemodeMessage.AddMarkupOrThrow(Loc.GetString("round-end-summary-window-round-id-label", ("roundId", roundId)));
        gamemodeMessage.AddText(" ");
        gamemodeMessage.AddMarkupOrThrow(Loc.GetString("round-end-summary-window-gamemode-name-label", ("gamemode", gamemode)));
        gamemodeLabel.SetMessage(gamemodeMessage);
        roundEndSummaryContainer.AddChild(gamemodeLabel);

        //Duration
        var roundTimeLabel = new RichTextLabel();
        roundTimeLabel.SetMarkup(Loc.GetString("round-end-summary-window-duration-label",
                                               ("hours", roundDuration.Hours),
                                               ("minutes", roundDuration.Minutes),
                                               ("seconds", roundDuration.Seconds)));
        roundEndSummaryContainer.AddChild(roundTimeLabel);

        //Round end text
        if (!string.IsNullOrEmpty(roundEnd))
        {
            var roundEndLabel = new RichTextLabel();
            roundEndLabel.SetMarkup(roundEnd);
            roundEndSummaryContainer.AddChild(roundEndLabel);
        }

        roundEndSummaryContainerScrollbox.AddChild(roundEndSummaryContainer);
        roundEndSummaryTab.AddChild(roundEndSummaryContainerScrollbox);

        return roundEndSummaryTab;
    }

    private BoxContainer MakePlayerManifestTab()
    {
        var playerManifestTab = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Name = Loc.GetString("round-end-summary-window-player-manifest-tab-title")
        };

        // Search container
        var searchContainer = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            Margin = new Thickness(10, 10, 10, 5)
        };

        var searchLabel = new Label
        {
            Text = "Filter: ",
            VerticalAlignment = VAlignment.Center,
            MinSize = new Vector2(40, 1)
        };

        var searchBar = new LineEdit
        {
            PlaceHolder = Loc.GetString("round-end-summary-window-player-manifest-tab-search-placeholder"),
            HorizontalExpand = true,
            MinSize = new Vector2(200, 1)
        };

        searchBar.OnTextChanged += OnSearchTextChanged;

        searchContainer.AddChild(searchLabel);
        searchContainer.AddChild(searchBar);
        playerManifestTab.AddChild(searchContainer);

        // Header with sort buttons
        var headerContainer = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            Margin = new Thickness(10, 5, 10, 5)
        };

        var icNameButton = CreateSortButton("round-end-summary-window-player-manifest-tab-sort-character", SortField.ICName);
        var roleButton = CreateSortButton("round-end-summary-window-player-manifest-tab-sort-role", SortField.Role);
        var playerTypeButton = CreateSortButton("round-end-summary-window-player-manifest-tab-sort-player-type", SortField.PlayerType);
        var oocNameButton = CreateSortButton("round-end-summary-window-player-manifest-tab-sort-player", SortField.OOCName);

        playerTypeButton.SetSortIndicator(true);
        headerContainer.AddChild(icNameButton);

        // Add small spacer between buttons
        headerContainer.AddChild(new Control
        {
            MinSize = new Vector2(5, 1),
            HorizontalExpand = false
        });

        headerContainer.AddChild(roleButton);

        // Add small spacer between buttons
        headerContainer.AddChild(new Control
        {
            MinSize = new Vector2(5, 1),
            HorizontalExpand = false
        });

        headerContainer.AddChild(playerTypeButton);

        // Add small spacer between buttons
        headerContainer.AddChild(new Control
        {
            MinSize = new Vector2(5, 1),
            HorizontalExpand = false
        });

        headerContainer.AddChild(oocNameButton);

        playerManifestTab.AddChild(headerContainer);

        var scrollContainer = new ScrollContainer
        {
            VerticalExpand = true,
            Margin = new Thickness(10, 0, 10, 10),
        };

        _playerGrid = new GridContainer
        {
            Columns = 1, // Goob Station - one card per player
            HorizontalExpand = true,
        };

        RefreshPlayerList();

        scrollContainer.AddChild(_playerGrid);
        playerManifestTab.AddChild(scrollContainer);

        return playerManifestTab;
    }

    private SortButton CreateSortButton(string text, SortField field)
    {
        var button = new SortButton(Loc.GetString(text), field);
        button.OnPressed += _ => SortBy(field);
        _sortButtons.Add(button);
        return button;
    }

    /// <summary>
    /// Handles sorting by the specified field, toggling direction if the same field is clicked
    /// </summary>
    private void SortBy(SortField field)
    {
        if (_currentSortField == field)
        {
            _sortDescending = !_sortDescending;
        }
        else
        {
            _currentSortField = field;
            _sortDescending = false;
        }

        foreach (var button in _sortButtons)
        {
            button.SetSortIndicator(button.Field == _currentSortField, _sortDescending);
        }

        RefreshPlayerList();
    }

    /// <summary>
    /// Refreshes the player list grid by clearing it and repopulating with sorted player data
    /// </summary>
    private void RefreshPlayerList()
    {
        _playerGrid.RemoveAllChildren();

        var sortedPlayers = GetSortedPlayers();
        foreach (var playerInfo in sortedPlayers)
        {
            AddPlayerRow(playerInfo);
        }
    }

    /// <summary>
    /// Adds a single player row to the grid with all columns (sprite, IC name, role, player type, OOC name)
    /// </summary>
    private void AddPlayerRow(RoundEndPlayerInfo playerInfo)
    {
        // Goob Station - End of Round Screen start
        // Each player is a card with their sprite, name/role, last words and how they died.
        var panel = new PanelContainer
        {
            StyleClasses = { StyleNano.StyleClassBackgroundBaseDark },
            Margin = new Thickness(0, 0, 0, 6)
        };

        var hBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            VerticalExpand = true
        };

        if (playerInfo.PlayerNetEntity != null)
        {
            hBox.AddChild(new SpriteView(playerInfo.PlayerNetEntity.Value, _entityManager)
            {
                OverrideDirection = Direction.South,
                VerticalAlignment = VAlignment.Center,
                SetSize = new Vector2(64, 64),
                VerticalExpand = true,
                Stretch = SpriteView.StretchMode.Fill,
                Margin = new Thickness(3, 0, 3, 0)
            });
        }

        var textVBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            VerticalExpand = true,
            SeparationOverride = 2,
        };

        var playerTitleBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
        };

        if (playerInfo.PlayerICName != null)
        {
            var playerNameText = new Label
            {
                VerticalAlignment = VAlignment.Bottom,
                StyleClasses = { StyleNano.StyleClassLabelHeading },
                Margin = new Thickness(0, 0, 6, 0),
                Text = playerInfo.PlayerICName
            };

            // Apply color coding for antagonists
            if (playerInfo.Antag)
                playerNameText.FontColorOverride = Color.Red;

            playerTitleBox.AddChild(playerNameText);

            var role = Loc.GetString(playerInfo.Role);
            var playerRoleText = new Label
            {
                VerticalAlignment = VAlignment.Bottom,
                StyleClasses = { StyleNano.StyleClassLabelSubText },
                Text = Loc.GetString("round-end-summary-window-player-name",
                    ("player", playerInfo.PlayerOOCName))
            };

            if (role != "Unknown")
                playerRoleText.Text = Loc.GetString("round-end-summary-window-player-name-role",
                        ("role", role), ("player", playerInfo.PlayerOOCName));

            playerTitleBox.AddChild(playerRoleText);
        }

        textVBox.AddChild(playerTitleBox);

        if (!string.IsNullOrWhiteSpace(playerInfo.LastWords))
        {
            var playerLastWordsText = new RichTextLabel
            {
                VerticalAlignment = VAlignment.Center,
                VerticalExpand = true,
            };

            playerLastWordsText.SetMarkup(Loc.GetString("round-end-summary-window-last-words",
                ("lastWords", playerInfo.LastWords)));

            textVBox.AddChild(playerLastWordsText);
        }

        var deathLabel = new RichTextLabel
        {
            VerticalAlignment = VAlignment.Center,
            VerticalExpand = true,
        };

        textVBox.AddChild(deathLabel);

        if (playerInfo.EntMobState == MobState.Dead
            && playerInfo.DamagePerGroup.Values.Any(v => v > 0))
        {
            var totalDamage = playerInfo.DamagePerGroup.Values.Sum(static v => (decimal) v);
            var severityAdj = totalDamage switch
            {
                >= 1000 => "catastrophic",
                >= 750 => "devastating",
                >= 500 => "agonizing",
                >= 300 => "painful",
                >= 200 => "brutal",
                _ => "tragic"
            };

            var highestDamage = playerInfo.DamagePerGroup
                .OrderByDescending(kvp => kvp.Value)
                .First();
            var typeAdj = highestDamage.Key switch
            {
                "Burn" => "fiery",
                "Brute" => "crushing",
                "Toxin" => "poisonous",
                "Airloss" => "suffocating",
                "Genetic" => "twisted",
                "Metaphysical" => "otherworldly",
                "Electronic" => "shocking",
                _ => "mysterious",
            };

            deathLabel.SetMarkup(
                Loc.GetString("round-end-summary-window-death",
                    ("severity", severityAdj),
                    ("type", typeAdj)));

            var damageTable = new GridContainer
            {
                Columns = playerInfo.DamagePerGroup.Count,
            };

            foreach (var damage in playerInfo.DamagePerGroup)
            {
                if (damage.Value <= 0)
                    continue;

                var color = damage.Key switch
                {
                    "Burn" => Color.Orange,
                    "Brute" => Color.Red,
                    "Toxin" => Color.Green,
                    "Airloss" => Color.Blue,
                    "Genetic" => Color.Cyan,
                    "Metaphysical" => Color.Purple,
                    "Electronic" => Color.DarkOrange,
                    _ => Color.White,
                };
                var damagePanel = new PanelContainer
                {
                    StyleClasses = { StyleNano.StyleClassBackgroundBaseLight },
                    Margin = new Thickness(2, 2, 2, 2)
                };
                var damageBox = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Margin = new Thickness(1)
                };
                var valueLabel = new Label
                {
                    Text = Math.Round((float) damage.Value).ToString(),
                    FontColorOverride = color,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                };
                var headerLabel = new Label
                {
                    Text = damage.Key,
                    FontColorOverride = Color.Gray,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                };
                damagePanel.AddChild(damageBox);
                damageBox.AddChild(valueLabel);
                damageBox.AddChild(headerLabel);
                damageTable.AddChild(damagePanel);
            }

            textVBox.AddChild(damageTable);
        }
        else if (playerInfo.EntMobState == MobState.Invalid)
        {
            deathLabel.SetMarkup(Loc.GetString("round-end-summary-window-death-unknown"));
        }

        hBox.AddChild(textVBox);
        panel.AddChild(hBox);
        _playerGrid.AddChild(panel);
        // Goob Station - End of Round Screen end
    }

    /// <summary>
    /// Gets the player type text for a player based on their observer and antagonist flags
    /// </summary>
    private static string GetPlayerTypeText(RoundEndPlayerInfo playerInfo)
    {
        if (playerInfo.Observer)
            return Loc.GetString("round-end-summary-window-player-manifest-tab-sort-player-type-observer");
        if (playerInfo.Antag)
            return Loc.GetString("round-end-summary-window-player-manifest-tab-sort-player-type-antag");

        return Loc.GetString("round-end-summary-window-player-manifest-tab-sort-player-type-crew");
    }

    private IEnumerable<RoundEndPlayerInfo> GetSortedPlayers()
    {
        // First filter players based on search text
        var filteredPlayers = string.IsNullOrEmpty(_searchText)
            ? _playersInfo
            : _playersInfo.Where(PlayerMatchesSearch);

        static string GetIcKey(RoundEndPlayerInfo p) =>
                (p.PlayerICName ?? p.PlayerOOCName).ToLowerInvariant();

        static string GetOocKey(RoundEndPlayerInfo p) =>
            p.PlayerOOCName.ToLowerInvariant();

        static string GetRoleKey(RoundEndPlayerInfo p) =>
            (p.Observer ? "zzz_observer" : p.Role).ToLowerInvariant();

        static int GetPlayerTypeSortKey(RoundEndPlayerInfo p) =>
            p.Antag ? 1 : p.Observer ? 3 : 2;

        return _currentSortField switch
        {
            SortField.ICName => ApplySort(filteredPlayers, GetIcKey, _sortDescending),
            SortField.OOCName => ApplySort(filteredPlayers, GetOocKey, _sortDescending),
            SortField.Role => ApplySort(filteredPlayers, GetRoleKey, _sortDescending),
            SortField.PlayerType => ApplySort(filteredPlayers, GetPlayerTypeSortKey, _sortDescending),
            _ => filteredPlayers
        };
    }

    private static IEnumerable<RoundEndPlayerInfo> ApplySort<TKey>(
        IEnumerable<RoundEndPlayerInfo> players,
        Func<RoundEndPlayerInfo, TKey> primaryKey,
        bool descending)
    {
        static string SecondaryKey(RoundEndPlayerInfo p) =>
            (p.PlayerICName ?? p.PlayerOOCName).ToLowerInvariant();

        return descending
            ? players.OrderByDescending(primaryKey).ThenByDescending(SecondaryKey)
            : players.OrderBy(primaryKey).ThenBy(SecondaryKey);
    }

    /// <summary>
    /// Gets a sort key for player type to ensure consistent ordering: Antagonist -> Crew -> Observer
    /// </summary>

    /// <summary>
    /// Checks if a player matches the current search filter
    /// </summary>
    private bool PlayerMatchesSearch(RoundEndPlayerInfo playerInfo)
    {
        if (string.IsNullOrEmpty(_searchText))
            return true;

        // Search in character name (IC name)
        if (!string.IsNullOrEmpty(playerInfo.PlayerICName) &&
            playerInfo.PlayerICName.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            return true;

        // Search in player name (OOC name)
        if (!string.IsNullOrEmpty(playerInfo.PlayerOOCName) &&
            playerInfo.PlayerOOCName.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            return true;

        // Search in role
        if (!string.IsNullOrEmpty(playerInfo.Role))
        {
            if (playerInfo.Role.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                Loc.GetString(playerInfo.Role).Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Search in player type
        var playerType = GetPlayerTypeText(playerInfo);
        if (playerType.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            return true;

        // Search for "Observer" when they are observers
        if (playerInfo.Observer && "observer".Contains(_searchText, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    /// <summary>
    /// Handles search text changes and refreshes the player list
    /// </summary>
    private void OnSearchTextChanged(LineEdit.LineEditEventArgs args)
    {
        _searchText = args.Text;
        RefreshPlayerList();
    }

    private sealed class SortButton : Button
    {
        public SortField Field { get; }
        private readonly Label _sortIndicator;

        public SortButton(string text, SortField field)
        {
            Field = field;
            HorizontalExpand = true;

            var container = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true
            };

            var label = new Label
            {
                Text = text,
                HorizontalExpand = true
            };

            _sortIndicator = new Label
            {
                Text = "",
                HorizontalAlignment = HAlignment.Right,
                MinSize = new Vector2(15, 1)
            };

            container.AddChild(label);
            container.AddChild(_sortIndicator);

            AddChild(container);
        }

        public void SetSortIndicator(bool active, bool descending = false)
        {
            if (!active)
            {
                _sortIndicator.Text = "";
                return;
            }

            _sortIndicator.Text = descending ? "▼" : "▲";
        }
    }
}
