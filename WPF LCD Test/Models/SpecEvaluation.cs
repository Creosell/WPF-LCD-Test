using System.Globalization;
using System.Text.Json;

namespace WPF_LCD_Test.Models
    {
    /// <summary>
    /// Status of one device metric checked against the device specification.
    /// </summary>
    public enum SpecStatus
        {
        /// <summary>The value meets the specification.</summary>
        Pass,

        /// <summary>The value is within min/max but below the typical value.</summary>
        Warning,

        /// <summary>The value is outside min/max.</summary>
        Fail,

        /// <summary>The points the metric needs are not measured yet.</summary>
        Pending,

        /// <summary>The points are measured, but the metric has no valid value.</summary>
        Unavailable
        }

    /// <summary>
    /// Verdict of a device over all evaluated checks.
    /// </summary>
    public enum SpecVerdict
        {
        /// <summary>No remarks.</summary>
        Pass,

        /// <summary>At least one value is below the typical value.</summary>
        Warn,

        /// <summary>At least one value is outside the specification.</summary>
        Fail
        }

    /// <summary>
    /// One checked metric of the device.
    /// </summary>
    /// <param name="Key">Metric key as in the device specification (e.g. "Brightness", "Red_x", "Cg_rgb_area").</param>
    /// <param name="Value">Value rounded to the report precision; null when pending or unavailable.</param>
    /// <param name="Min">Specification minimum, if any.</param>
    /// <param name="Typ">Specification typical value, if any.</param>
    /// <param name="Max">Specification maximum, if any.</param>
    /// <param name="Status">Check status.</param>
    /// <param name="Locations">Measurement locations the metric comes from.</param>
    /// <param name="Missing">Locations that are not measured yet.</param>
    public sealed record SpecCheck(
        string Key,
        double? Value,
        double? Min,
        double? Typ,
        double? Max,
        SpecStatus Status,
        IReadOnlyList<string> Locations,
        IReadOnlyList<string> Missing);

    /// <summary>
    /// What the specification check found out about the attempts of one point.
    /// </summary>
    /// <param name="Count">Number of accepted attempts.</param>
    /// <param name="Best">Index of the best attempt (the one the evaluation used).</param>
    /// <param name="Deviated">True when any attempt had a remark against the specification.</param>
    public sealed record SpecAttemptInfo(int Count, int Best, bool Deviated);

    /// <summary>
    /// Result of evaluating the measurements of a device against its specification.
    /// </summary>
    /// <param name="Verdict">Verdict over the evaluated checks.</param>
    /// <param name="Complete">True when no check is pending.</param>
    /// <param name="Checks">All checks that have a specification.</param>
    public sealed record SpecEvaluation(SpecVerdict Verdict, bool Complete, IReadOnlyList<SpecCheck> Checks)
        {
        /// <summary>
        /// Gets the attempt summary per measurement location; empty when the generator did not report it.
        /// </summary>
        public IReadOnlyDictionary<string, SpecAttemptInfo> Attempts { get; init; } = new Dictionary<string, SpecAttemptInfo>();

        /// <summary>
        /// Parses the JSON printed by the report generator in the evaluate mode.
        /// </summary>
        /// <param name="json">Generator output.</param>
        /// <returns>The evaluation.</returns>
        /// <exception cref="FormatException">Thrown when the JSON is not an evaluation result.</exception>
        public static SpecEvaluation Parse(string json)
            {
            try
                {
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                if (root.TryGetProperty("error", out var error))
                    {
                    throw new FormatException(error.GetString());
                    }

                var checks = root.GetProperty("checks").EnumerateArray().Select(ParseCheck).ToList();
                return new SpecEvaluation(ParseVerdict(root.GetProperty("device").GetString()), root.GetProperty("complete").GetBoolean(), checks) { Attempts = ParseAttempts(root) };
                }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
                {
                throw new FormatException($"Unexpected evaluation result: {ex.Message}", ex);
                }
            }

        private static Dictionary<string, SpecAttemptInfo> ParseAttempts(JsonElement root)
            {
            var attempts = new Dictionary<string, SpecAttemptInfo>();
            if (!root.TryGetProperty("attempts", out var element) || element.ValueKind != JsonValueKind.Object) return attempts;

            foreach (var property in element.EnumerateObject())
                {
                var info = property.Value;
                attempts[property.Name] = new SpecAttemptInfo(
                    info.GetProperty("count").GetInt32(),
                    info.GetProperty("best").GetInt32(),
                    info.GetProperty("deviated").GetBoolean());
                }
            return attempts;
            }

        private static SpecCheck ParseCheck(JsonElement element) => new(
            element.GetProperty("key").GetString() ?? string.Empty,
            ReadNumber(element, "value"),
            ReadNumber(element, "min"),
            ReadNumber(element, "typ"),
            ReadNumber(element, "max"),
            ParseStatus(element.GetProperty("status").GetString()),
            ReadStrings(element, "locations"),
            ReadStrings(element, "missing"));

        private static double? ReadNumber(JsonElement element, string name)
            {
            if (!element.TryGetProperty(name, out var value)) return null;
            if (value.ValueKind == JsonValueKind.Number) return value.GetDouble();
            return value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
            }

        private static IReadOnlyList<string> ReadStrings(JsonElement element, string name) =>
            element.TryGetProperty(name, out var array) && array.ValueKind == JsonValueKind.Array
                ? array.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToList()
                : [];

        private static SpecStatus ParseStatus(string? status) => status switch
            {
            "pass" => SpecStatus.Pass,
            "warning" => SpecStatus.Warning,
            "fail" => SpecStatus.Fail,
            "pending" => SpecStatus.Pending,
            "n/a" => SpecStatus.Unavailable,
            _ => throw new FormatException($"Unknown status '{status}'.")
            };

        private static SpecVerdict ParseVerdict(string? verdict) => verdict switch
            {
            "PASS" => SpecVerdict.Pass,
            "WARN" => SpecVerdict.Warn,
            "FAIL" => SpecVerdict.Fail,
            _ => throw new FormatException($"Unknown verdict '{verdict}'.")
            };
        }
    }
