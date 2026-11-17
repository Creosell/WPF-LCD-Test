using MvvmHelpers;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.ViewModels
    {
    /// <summary>
    /// Presents a <see cref="SpecEvaluation"/>: the device info table, the verdict, the status badges of the
    /// measurement buttons and the log lines about new deviations.
    /// </summary>
    public partial class SpecSummaryViewModel : BaseViewModel
        {
        private const string Dash = "—";

        private static readonly Dictionary<string, string> GamutSpaces = new()
            {
            ["rgb"] = "sRGB",
            ["ntsc"] = "NTSC",
            ["dcip3"] = "DCI-P3",
            ["rec2020"] = "Rec.2020"
            };

        private readonly IMeasurementStatusService _statusService;
        private readonly Action<string, LogSeverity> _log;
        private readonly Dictionary<string, SpecStatus> _loggedStatuses = [];

        private SpecEvaluation? _evaluation;
        private int _measuredPoints;
        private int _totalPoints;
        private string _message = string.Empty;
        private string _verdictText = string.Empty;
        private string _progressText = string.Empty;
        private string _hintText = string.Empty;
        private SpecVerdict? _verdict;

        /// <summary>
        /// Initializes a new instance of SpecSummaryViewModel.
        /// </summary>
        /// <param name="statusService">Service owning the statuses of the measurement buttons that receive the badges.</param>
        /// <param name="log">Writes a line with its severity to the application log.</param>
        public SpecSummaryViewModel(IMeasurementStatusService statusService, Action<string, LogSeverity> log)
            {
            _statusService = statusService;
            _log = log;
            }

        /// <summary>
        /// Gets the rows of the device info table (every checked metric except the coordinates, which are shown on the buttons).
        /// </summary>
        public ObservableCollection<SpecRowViewModel> Rows { get; } = [];

        /// <summary>
        /// Gets the verdict of the device; null when there is nothing to show.
        /// </summary>
        public SpecVerdict? Verdict
            {
            get => _verdict;
            private set => SetProperty(ref _verdict, value);
            }

        /// <summary>
        /// Gets the verdict chip text, empty when there is no evaluation.
        /// </summary>
        public string VerdictText
            {
            get => _verdictText;
            private set => SetProperty(ref _verdictText, value);
            }

        /// <summary>
        /// Gets the "N of M points" text.
        /// </summary>
        public string ProgressText
            {
            get => _progressText;
            private set => SetProperty(ref _progressText, value);
            }

        /// <summary>
        /// Gets the message shown instead of the table (no configuration selected, the check is unavailable); empty otherwise.
        /// </summary>
        public string HintText
            {
            get => _hintText;
            private set => SetProperty(ref _hintText, value);
            }

        /// <summary>
        /// Gets the evaluation that is currently shown.
        /// </summary>
        public SpecEvaluation? Evaluation => _evaluation;

        /// <summary>
        /// Shows an evaluation: updates the table, the verdict, the badges and logs the new deviations.
        /// </summary>
        /// <param name="evaluation">Result of the specification check.</param>
        /// <param name="measuredPoints">Number of points measured so far.</param>
        /// <param name="totalPoints">Number of points the device needs.</param>
        public void Show(SpecEvaluation evaluation, int measuredPoints, int totalPoints)
            {
            _evaluation = evaluation;
            _measuredPoints = measuredPoints;
            _totalPoints = totalPoints;
            _message = string.Empty;
            Render();
            LogNewDeviations(evaluation);
            }

        /// <summary>
        /// Shows a message instead of the table (no configuration selected, the check is unavailable).
        /// </summary>
        /// <param name="message">Localized message.</param>
        public void ShowMessage(string message)
            {
            Clear();
            _message = message;
            HintText = message;
            }

        /// <summary>
        /// Clears the table, the verdict, the badges and the log memory.
        /// </summary>
        public void Reset()
            {
            Clear();
            _message = string.Empty;
            HintText = string.Empty;
            }

        /// <summary>
        /// Rebuilds the texts of the current evaluation (after the language changed).
        /// </summary>
        public void Refresh()
            {
            if (_evaluation != null) Render();
            }

        /// <summary>
        /// Describes the checks that are outside the specification, one line each.
        /// </summary>
        /// <returns>Lines such as "Red (x) 0.701 &gt; max 0.689"; empty when nothing fails.</returns>
        public IReadOnlyList<string> DescribeFailures() =>
            _evaluation?.Checks.Where(check => check.Status == SpecStatus.Fail).Select(Describe).ToList() ?? [];

        private void Clear()
            {
            _evaluation = null;
            _loggedStatuses.Clear();
            Rows.Clear();
            Verdict = null;
            VerdictText = string.Empty;
            ProgressText = string.Empty;
            ClearBadges();
            }

        private void Render()
            {
            var evaluation = _evaluation!;

            Rows.Clear();
            foreach (var check in evaluation.Checks.Where(check => !IsCoordinate(check.Key)))
                {
                Rows.Add(new SpecRowViewModel(NameOf(check.Key), FormatNumber(check.Value), FormatBounds(check), check.Status, RowToolTip(check)));
                }

            Verdict = evaluation.Verdict;
            VerdictText = string.Format(Text("SpecVerdictFormat"), VerdictName(evaluation));
            ProgressText = string.Format(Text("SpecPointsProgress"), _measuredPoints, _totalPoints);
            HintText = string.Empty;
            ApplyBadges(evaluation);
            }

        private string VerdictName(SpecEvaluation evaluation) => evaluation.Verdict switch
            {
            SpecVerdict.Fail => Text("SpecVerdictFail"),
            SpecVerdict.Warn => Text("SpecVerdictWarn"),
            _ => evaluation.Complete ? Text("SpecVerdictPass") : Text("SpecNoIssuesYet")
            };

        private string? RowToolTip(SpecCheck check) => check.Status switch
            {
            SpecStatus.Pending => string.Format(Text("SpecWaitingFor"), string.Join(", ", check.Missing.Select(LocationName))),
            SpecStatus.Warning or SpecStatus.Fail => Describe(check),
            _ => null
            };

        private void ClearBadges()
            {
            foreach (var status in _statusService.AllMeasurementButtonStatuses)
                {
                status.SpecRemark = SpecStatus.Pass;
                status.SpecTooltip = string.Empty;
                }
            }

        private void ApplyBadges(SpecEvaluation evaluation)
            {
            ClearBadges();

            var remarks = evaluation.Checks
                .Where(check => check.Locations.Count == 1 && check.Status is SpecStatus.Warning or SpecStatus.Fail)
                .GroupBy(check => check.Locations[0]);
            foreach (var group in remarks)
                {
                var status = _statusService.AllMeasurementButtonStatuses.FirstOrDefault(s => s.Location == group.Key);
                if (status == null) continue;

                var ordered = group.OrderByDescending(check => check.Status).ThenBy(check => check.Key).ToList();
                status.SpecRemark = ordered[0].Status;
                status.SpecTooltip = string.Join(Environment.NewLine, ordered.Select(Describe));
                }
            }

        private void LogNewDeviations(SpecEvaluation evaluation)
            {
            foreach (var check in evaluation.Checks)
                {
                bool isRemark = check.Status is SpecStatus.Warning or SpecStatus.Fail;
                bool isNew = !_loggedStatuses.TryGetValue(check.Key, out var previous) || previous != check.Status;
                if (isRemark && isNew)
                    {
                    _log(string.Format(Text("SpecLogLine"), Describe(check)), check.Status == SpecStatus.Fail ? LogSeverity.Error : LogSeverity.Warning);
                    }
                _loggedStatuses[check.Key] = check.Status;
                }
            }

        private string Describe(SpecCheck check)
            {
            var name = NameOf(check.Key);
            var value = FormatNumber(check.Value);
            bool lowerIsBetter = check.Key == "Delta_e";

            if (check.Status == SpecStatus.Fail)
                {
                if (check.Min is double min && check.Value < min) return $"{name} {value} < {Text("SpecBound_min")} {FormatNumber(min)}";
                if (check.Max is double max && check.Value > max) return $"{name} {value} > {Text("SpecBound_max")} {FormatNumber(max)}";
                }
            if (check.Typ is double typ)
                {
                return lowerIsBetter ? $"{name} {value} > {Text("SpecBound_typ")} {FormatNumber(typ)}" : $"{name} {value} < {Text("SpecBound_typ")} {FormatNumber(typ)}";
                }
            return $"{name} {value}";
            }

        private static string FormatBounds(SpecCheck check)
            {
            var parts = new List<string>();
            if (check.Min is double min) parts.Add($"{Text("SpecBound_min")} {FormatNumber(min)}");
            if (check.Typ is double typ) parts.Add($"{Text("SpecBound_typ")} {FormatNumber(typ)}");
            if (check.Max is double max) parts.Add($"{Text("SpecBound_max")} {FormatNumber(max)}");
            return string.Join(" · ", parts);
            }

        private static string FormatNumber(double? value) => value?.ToString("0.###", CultureInfo.InvariantCulture) ?? Dash;

        private static bool IsCoordinate(string key) => CoordinateKey().IsMatch(key);

        [GeneratedRegex("^(Red|Green|Blue|White|Center)_[xy]$")]
        private static partial Regex CoordinateKey();

        [GeneratedRegex("^Cg_(?<space>[a-z0-9]+?)(?<uv>_uv)?(?<area>_area)?$")]
        private static partial Regex GamutKey();

        private static string NameOf(string key)
            {
            var gamut = GamutKey().Match(key);
            if (gamut.Success && GamutSpaces.TryGetValue(gamut.Groups["space"].Value, out var space))
                {
                var diagram = gamut.Groups["uv"].Success ? "CIE 1976" : "CIE 1931";
                return string.Format(Text(gamut.Groups["area"].Success ? "SpecGamutArea" : "SpecGamutCoverage"), space, diagram);
                }

            var name = Resources.Resources.ResourceManager.GetString($"SpecMetric_{key}");
            return string.IsNullOrEmpty(name) ? key : name;
            }

        private static string LocationName(string location)
            {
            var name = Resources.Resources.ResourceManager.GetString($"SpecLocation_{location}");
            return string.IsNullOrEmpty(name) ? location : name;
            }

        private static string Text(string key) => Resources.Resources.ResourceManager.GetString(key) ?? key;
        }
    }
