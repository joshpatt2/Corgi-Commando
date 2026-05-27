using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace CorgiCommando.Testing
{
    public sealed class PlaytestRecorder : IDisposable
    {
        private const string TimestampFormat = "yyyyMMdd-HHmmssfff";
        private readonly Func<DateTime> _utcNowProvider;
        private readonly Action<string> _captureScreenshot;
        private readonly List<RecordedEvent> _recordedEvents = new List<RecordedEvent>();

        private string _runDirectory;
        private bool _isSubscribed;
        private bool _isDisposed;
        private int _sequence;
        private DateTime _runStartUtc;

        private readonly struct RecordedEvent
        {
            public RecordedEvent(DateTime timestampUtc, string eventName, string componentId, string oldState, string newState, string screenshotPath)
            {
                TimestampUtc = timestampUtc;
                EventName = eventName;
                ComponentId = componentId;
                OldState = oldState;
                NewState = newState;
                ScreenshotPath = screenshotPath;
            }

            public DateTime TimestampUtc { get; }
            public string EventName { get; }
            public string ComponentId { get; }
            public string OldState { get; }
            public string NewState { get; }
            public string ScreenshotPath { get; }
        }

        public PlaytestRecorder()
            : this(() => DateTime.UtcNow, ScreenCapture.CaptureScreenshot)
        {
        }

        public PlaytestRecorder(Func<DateTime> utcNowProvider, Action<string> captureScreenshot)
        {
            _utcNowProvider = utcNowProvider ?? throw new ArgumentNullException(nameof(utcNowProvider));
            _captureScreenshot = captureScreenshot ?? throw new ArgumentNullException(nameof(captureScreenshot));
        }

        public void StartRun()
        {
            ThrowIfDisposed();
            if (_isSubscribed)
            {
                return;
            }

            _runStartUtc = _utcNowProvider();
            _runDirectory = Path.Combine(GetProjectRootPath(), "Builds", "playtest-runs", _runStartUtc.ToString(TimestampFormat, CultureInfo.InvariantCulture));
            Directory.CreateDirectory(_runDirectory);

            PlaytestMetrics.StateTransitionLogged += HandleStateTransitionLogged;
            _isSubscribed = true;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            StopRun();
            _isDisposed = true;
        }

        private void StopRun()
        {
            if (!_isSubscribed)
            {
                return;
            }

            PlaytestMetrics.StateTransitionLogged -= HandleStateTransitionLogged;
            _isSubscribed = false;
            WriteSummary();
        }

        private void HandleStateTransitionLogged(PlaytestMetrics.StateTransitionEntry transition)
        {
            if (!TryMatchEvent(transition, out string eventName))
            {
                return;
            }

            _sequence++;
            string fileName = $"frame-{eventName}-{_sequence:D3}.png";
            string screenshotPath = Path.Combine(_runDirectory, fileName);
            _captureScreenshot(screenshotPath);

            DateTime eventTimestampUtc = _utcNowProvider();
            _recordedEvents.Add(new RecordedEvent(
                eventTimestampUtc,
                eventName,
                transition.componentId ?? string.Empty,
                transition.oldState ?? string.Empty,
                transition.newState ?? string.Empty,
                screenshotPath));
        }

        private void WriteSummary()
        {
            if (string.IsNullOrEmpty(_runDirectory))
            {
                return;
            }

            string summaryPath = Path.Combine(_runDirectory, "summary.md");
            var markdown = new StringBuilder();
            markdown.AppendLine($"# Playtest Run {_runStartUtc:O}");
            markdown.AppendLine();
            markdown.AppendLine("| Timestamp (UTC) | Event | Transition | Screenshot |");
            markdown.AppendLine("| --- | --- | --- | --- |");

            for (int i = 0; i < _recordedEvents.Count; i++)
            {
                RecordedEvent runEvent = _recordedEvents[i];
                markdown.Append("| ");
                markdown.Append(runEvent.TimestampUtc.ToString("O", CultureInfo.InvariantCulture));
                markdown.Append(" | ");
                markdown.Append(runEvent.EventName);
                markdown.Append(" | ");
                markdown.Append(runEvent.ComponentId);
                markdown.Append(": ");
                markdown.Append(runEvent.OldState);
                markdown.Append(" → ");
                markdown.Append(runEvent.NewState);
                markdown.Append(" | ");
                markdown.Append(runEvent.ScreenshotPath);
                markdown.AppendLine(" |");
            }

            File.WriteAllText(summaryPath, markdown.ToString());
        }

        private static string GetProjectRootPath()
        {
            string assetsPath = Application.dataPath;
            if (string.IsNullOrEmpty(assetsPath))
            {
                return Directory.GetCurrentDirectory();
            }

            string projectRoot = Path.GetDirectoryName(assetsPath);
            return string.IsNullOrEmpty(projectRoot) ? Directory.GetCurrentDirectory() : projectRoot;
        }

        private static bool TryMatchEvent(PlaytestMetrics.StateTransitionEntry transition, out string eventName)
        {
            string component = (transition.componentId ?? string.Empty).ToLowerInvariant();
            string oldState = (transition.oldState ?? string.Empty).ToLowerInvariant();
            string newState = (transition.newState ?? string.Empty).ToLowerInvariant();

            if (ContainsAny(newState, "victory", "encountercomplete", "runcomplete"))
            {
                eventName = "victory";
                return true;
            }

            if (ContainsAny(newState, "waveclear", "wave-cleared", "cleared"))
            {
                eventName = "wave-clear";
                return true;
            }

            if (ContainsAny(newState, "wavestart", "wave-start"))
            {
                eventName = "wave-start";
                return true;
            }

            if (ContainsAny(newState, "bossintro", "boss-intro"))
            {
                eventName = "boss-intro";
                return true;
            }

            if (component.Contains("whiskerbot") && oldState == "0" && newState == "1")
            {
                eventName = "boss-intro";
                return true;
            }

            if (ContainsAny(newState, "phase"))
            {
                eventName = "boss-phase-transition";
                return true;
            }

            if (component.Contains("whiskerbot") &&
                int.TryParse(oldState, NumberStyles.Integer, CultureInfo.InvariantCulture, out int oldPhase) &&
                int.TryParse(newState, NumberStyles.Integer, CultureInfo.InvariantCulture, out int newPhase) &&
                oldPhase != newPhase)
            {
                eventName = "boss-phase-transition";
                return true;
            }

            eventName = string.Empty;
            return false;
        }

        private static bool ContainsAny(string value, params string[] candidates)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                if (value.Contains(candidates[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(PlaytestRecorder));
            }
        }
    }
}
