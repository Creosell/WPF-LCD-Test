using MvvmHelpers;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.ViewModels
    {
    /// <summary>
    /// A remark against the specification for the confirmation of saving.
    /// </summary>
    /// <param name="Text">The description of the remark.</param>
    /// <param name="Status">Warning (below typ) or fail (outside min/max).</param>
    /// <param name="FewAttempts">True when the point was measured fewer than three times.</param>
    public sealed record SpecProblem(string Text, SpecStatus Status, bool FewAttempts);

    /// <summary>
    /// Presents a <see cref="SpecEvaluation"/>: the device info table, the verdict, the status badges of the
    /// measurement buttons and the log lines about new deviations.
    /// </summary>
    public partial class SpecSummaryViewModel : BaseViewModel
        {
        private const string Dash = "—";

        /// <summary>A check of more points than this (the uniformity over nine) would mark half of the buttons and says nothing about any of them.</summary>
        private const int MaxBadgePoints = 3;

        /// <summary>The metrics where a lower value is better: their bounds are read the other way round.</summary>
        private static bool IsLowerBetter(string key) => key is "Delta_e" or "Black_luminance";

        private static readonly Dictionary<string, string> GamutSpaces = new()
            {
            ["rgb"] = "sRGB",
            ["ntsc"] = "NTSC",
            ["dcip3"] = "DCI-P3",
            ["rec2020"] = "Rec.2020"
            };

        private readonly IMeasurementStatusService _statusService;
        private readonly Action<string, LogSeverity> _log;
        private readonly Dictionary<string, (SpecStatus Status, int Attempts)> _loggedStatuses = [];

        private SpecEvaluation? _evaluation;
        private int _measuredPoints;
        private int _totalPoints;
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
            HintText = message;
            }

        /// <summary>
        /// Clears the table, the verdict, the badges and the log memory.
        /// </summary>
        public void Reset()
            {
            Clear();
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
        /// Lists the remarks against the specification for the confirmation of saving: the checks outside min/max first,
        /// then those below typ. A check of one point names the point and how many times it was measured.
        /// </summary>
        /// <returns>The problems; empty when there is none.</returns>
        public IReadOnlyList<SpecProblem> DescribeProblems()
            {
            if (_evaluation == null) return [];

            return _evaluation.Checks
                .Where(check => check.Status is SpecStatus.Fail or SpecStatus.Warning)
                .OrderByDescending(check => check.Status)
                .Select(check =>
                    {
                    int attempts = AttemptsOf(_evaluation, check);
                    string text = attempts > 0
                        ? string.Format(Text("SaveProblemPoint"), LocationName(check.Locations[0]), DescribeInWords(check), MeasuredText(attempts))
                        : check.Locations.Count > 1 ? DescribeWithPoints(_evaluation, check) : DescribeInWords(check);
                    return new SpecProblem(text, check.Status, FewAttemptsOf(_evaluation, check));
                    })
                .ToList();
            }

        /// <summary>
        /// Tells whether a point the check comes from was measured fewer than three times; a point that already has three attempts
        /// is never asked to be measured again.
        /// </summary>
        private static bool FewAttemptsOf(SpecEvaluation evaluation, SpecCheck check) =>
            check.Locations.Any(location => evaluation.Attempts.TryGetValue(location, out var info) && info.Count is > 0 and < MeasurementResultRow.EnoughAttempts);

        /// <summary>
        /// Describes a check of several points and names each point with how many times it was measured, e.g.
        /// "Contrast ratio is 3000, below the minimum 4125 (White: measured once, Black: measured 3 times)".
        /// </summary>
        private string DescribeWithPoints(SpecEvaluation evaluation, SpecCheck check)
            {
            var points = check.Locations
                .Where(location => evaluation.Attempts.ContainsKey(location))
                .Select(location => $"{LocationName(location)}: {MeasuredText(evaluation.Attempts[location].Count)}")
                .ToList();
            return points.Count == 0 ? DescribeInWords(check) : $"{DescribeInWords(check)} ({string.Join(", ", points)})";
            }

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
                .Where(check => check.Status is SpecStatus.Warning or SpecStatus.Fail && check.Locations.Count is > 0 and <= MaxBadgePoints)
                .SelectMany(check => check.Locations.Select(location => (Location: location, Check: check)))
                .GroupBy(item => item.Location);
            foreach (var group in remarks)
                {
                var status = _statusService.AllMeasurementButtonStatuses.FirstOrDefault(s => s.Location == group.Key);
                if (status == null) continue;

                var ordered = group.Select(item => item.Check).OrderByDescending(check => check.Status).ThenBy(check => check.Key).ToList();
                status.SpecRemark = ordered[0].Status;
                status.SpecTooltip = string.Join(Environment.NewLine, ordered.Select(BadgeLine));
                }
            }

        /// <summary>
        /// A line of a badge tooltip: what is wrong; a check of several points adds the points it comes from, e.g. "Contrast ratio 3000 &lt; min 4125 (White + Black)".
        /// </summary>
        private string BadgeLine(SpecCheck check) =>
            check.Locations.Count > 1 ? $"{Describe(check)} ({string.Join(" + ", check.Locations.Select(LocationName))})" : Describe(check);

        private void LogNewDeviations(SpecEvaluation evaluation)
            {
            foreach (var check in evaluation.Checks)
                {
                bool isRemark = check.Status is SpecStatus.Warning or SpecStatus.Fail;
                var current = (check.Status, Attempts: AttemptsOf(evaluation, check));
                bool isNew = !_loggedStatuses.TryGetValue(check.Key, out var previous) || previous != current;
                if (isRemark && isNew)
                    {
                    _log(LogLine(check, current.Attempts), check.Status == SpecStatus.Fail ? LogSeverity.Error : LogSeverity.Warning);
                    }
                _loggedStatuses[check.Key] = current;
                }
            }

        /// <summary>
        /// Builds the log line of a remark: the point (when the name of the metric does not already carry it), what is wrong,
        /// the attempt and, outside min/max, the advice to repeat, e.g. "Center: Brightness (cd/m²) 251 &lt; min 290 (attempt 1). Repeat the measurement".
        /// </summary>
        private string LogLine(SpecCheck check, int attempts)
            {
            string line = Describe(check);
            if (attempts > 0)
                {
                string point = LocationName(check.Locations[0]);
                string name = NameOf(check.Key);
                line = name.StartsWith(point, StringComparison.Ordinal) ? line : $"{point}: {line}";
                line = $"{line} ({string.Format(Text("SpecAttemptSuffix"), attempts)})";
                }
            return check.Status == SpecStatus.Fail ? $"{line}. {Text("SpecRepeatRecommended")}" : line;
            }

        /// <summary>
        /// Returns the number of attempts of the point a single-point check comes from; 0 for other checks or when unknown.
        /// </summary>
        private static int AttemptsOf(SpecEvaluation evaluation, SpecCheck check) =>
            check.Locations.Count == 1 && evaluation.Attempts.TryGetValue(check.Locations[0], out var info) ? info.Count : 0;

        private static string MeasuredText(int attempts) =>
            attempts == 1 ? Text("SaveProblemMeasuredOnce") : string.Format(Text("SaveProblemMeasuredTimes"), attempts);

        /// <summary>
        /// Describes what is wrong with a check in plain words for the operator, e.g. "Brightness (cd/m²) is 42, below the minimum 290".
        /// </summary>
        private string DescribeInWords(SpecCheck check)
            {
            var name = NameOf(check.Key);
            var value = FormatNumber(check.Value);

            if (check.Status == SpecStatus.Fail)
                {
                if (IsLowerBetter(check.Key))
                    {
                    if (WorstBoundBelow(check) is var (_, bound)) return string.Format(Text("SaveProblemAboveMax"), name, value, FormatNumber(bound));
                    }
                else
                    {
                    if (check.Min is double min && check.Value < min) return string.Format(Text("SaveProblemBelowMin"), name, value, FormatNumber(min));
                    if (check.Max is double max && check.Value > max) return string.Format(Text("SaveProblemAboveMax"), name, value, FormatNumber(max));
                    }
                }
            if (check.Typ is double typ)
                {
                return string.Format(Text(IsLowerBetter(check.Key) ? "SaveProblemAboveTyp" : "SaveProblemBelowTyp"), name, value, FormatNumber(typ));
                }
            return $"{name} {value}";
            }

        private string Describe(SpecCheck check)
            {
            var name = NameOf(check.Key);
            var value = FormatNumber(check.Value);
            bool lowerIsBetter = IsLowerBetter(check.Key);

            if (check.Status == SpecStatus.Fail)
                {
                if (lowerIsBetter)
                    {
                    if (WorstBoundBelow(check) is var (label, bound)) return $"{name} {value} > {Text($"SpecBound_{label}")} {FormatNumber(bound)}";
                    }
                else
                    {
                    if (check.Min is double min && check.Value < min) return $"{name} {value} < {Text("SpecBound_min")} {FormatNumber(min)}";
                    if (check.Max is double max && check.Value > max) return $"{name} {value} > {Text("SpecBound_max")} {FormatNumber(max)}";
                    }
                }
            if (check.Typ is double typ)
                {
                return lowerIsBetter ? $"{name} {value} > {Text("SpecBound_typ")} {FormatNumber(typ)}" : $"{name} {value} < {Text("SpecBound_typ")} {FormatNumber(typ)}";
                }
            return $"{name} {value}";
            }

        /// <summary>
        /// For a metric where lower is better, finds the bound the value is above that fails it: the largest such bound
        /// (a spec lists them as min 4, typ 3, max 2 for the color difference, so "min" is the worst allowed one).
        /// </summary>
        private static (string Label, double Bound)? WorstBoundBelow(SpecCheck check)
            {
            var exceeded = new[] { ("min", check.Min), ("max", check.Max) }
                .Where(bound => bound.Item2 is double limit && check.Value > limit)
                .OrderByDescending(bound => bound.Item2)
                .ToList();
            return exceeded.Count == 0 ? null : (exceeded[0].Item1, exceeded[0].Item2!.Value);
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
