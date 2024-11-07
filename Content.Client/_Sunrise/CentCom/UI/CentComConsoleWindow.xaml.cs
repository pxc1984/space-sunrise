﻿using Content.Client.UserInterface.Controls;
using Content.Shared._Sunrise.CentCom.BUIStates;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.CentCom.UI;

[GenerateTypedNameReferences]
public sealed partial class CentComConsoleWindow : FancyWindow
{
    public TimeSpan? LastTime;
    public event Action<string>? OnAnnounce;
    public event Action<string>? OnAlertLevel;
    public event Action<object?>? OnEmergencyShuttle;
    private bool SelectedTimeFlag = false;

    public CentComConsoleWindow()
    {
        RobustXamlLoader.Load(this);

        // А вот так вот
        Resizable = false;

        AnnounceButton.OnPressed += _ => OnAnnounce?.Invoke(Rope.Collapse(MessageInput.TextRope));

        AlertLevelButton.OnItemSelected += args =>
        {
            var metadata = AlertLevelButton.GetItemMetadata(args.Id);
            if (metadata != null && metadata is string cast)
            {
                OnAlertLevel?.Invoke(cast);
            }
        };

        DurationButton.OnItemSelected += args =>
        {
            DurationButton.SelectId(args.Id);
            SelectedTimeFlag = true;
        };

        EmergencyShuttleButton.OnPressed += _ =>
            OnEmergencyShuttle?.Invoke(DurationButton.SelectedMetadata);
    }

    public void UpdateState(CentComConsoleBoundUserInterfaceState state)
    {
        IdButton.Text = state.IsIdPresent
            ? Loc.GetString("id-card-console-window-eject-button")
            : Loc.GetString("id-card-console-window-insert-button");
        IdLabel.Text = state.IdName != string.Empty
            ? state.IdName
            : Loc.GetString("centcom-console-window-empty-id");
        if (state.Station != null)
        {
            StationNameLabel.Text = state.Station.Name;

            AlertLevelButton.Clear();
            foreach (var alertLevel in state.Station.AlertLevels)
            {
                var name = alertLevel;
                if (Loc.TryGetString($"alert-level-{alertLevel}", out var locName))
                {
                    name = locName;
                }
                AlertLevelButton.AddItem(name);
                AlertLevelButton.SetItemMetadata(AlertLevelButton.ItemCount - 1, alertLevel);

                if (alertLevel == state.Station.CurrentAlert)
                {
                    AlertLevelButton.Select(AlertLevelButton.ItemCount - 1);
                }
            }

            DurationButton.Clear();
            foreach (var del in state.Station.Delays)
            {
                var delay = del;
                if (Loc.TryGetString($"centcom-console-duration-{delay.Label}", out var locDelay))
                {
                    delay.Label = locDelay;
                }
                DurationButton.AddItem(delay.Label);
                DurationButton.SetItemMetadata(DurationButton.ItemCount - 1, delay);

                if (delay.Time == state.Station.DefaultDelay && !SelectedTimeFlag)
                {
                    DurationButton.Select(DurationButton.ItemCount - 1);
                }
            }

            if (!state.SentEvac)
            {
                EmergencyShuttleButton.Text = Loc.GetString("centcom-console-call-shuttle-label");
                LastTime = null;
            }
            else
            {
                if (state.LeftBeforeEvac != null)
                {
                    EmergencyShuttleButton.Text = $"Осталось {(int)Math.Floor(state.LeftBeforeEvac.Value.TotalSeconds)}с";
                    LastTime = state.LeftBeforeEvac;
                }
            }
        }

        AnnounceButton.Disabled = !(state.IsIdPresent && state.IdEnoughPermissions);
        DurationButton.Disabled = !(state.IsIdPresent && state.IdEnoughPermissions);
        EmergencyShuttleButton.Disabled = !(state.IsIdPresent && state.IdEnoughPermissions);
        AlertLevelButton.Disabled = !(state.IsIdPresent && state.IdEnoughPermissions);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (LastTime == null)
            return;
        EmergencyShuttleButton.Text = $"Осталось {(int)Math.Floor((LastTime.Value - TimeSpan.FromSeconds(args.DeltaSeconds)).TotalSeconds)}с";
        LastTime = LastTime.Value - TimeSpan.FromSeconds(args.DeltaSeconds);
    }
}
