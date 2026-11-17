using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Interfaces
    {
    /// <summary>
    /// Result of a specification check request: the evaluation, or the reason it is unavailable.
    /// </summary>
    /// <param name="Evaluation">The evaluation; null when it could not be obtained.</param>
    /// <param name="Error">Why the evaluation is unavailable; null on success.</param>
    public readonly record struct SpecEvaluationOutcome(SpecEvaluation? Evaluation, string? Error);

    /// <summary>
    /// Checks the measurements of a device against its specification using the report generator,
    /// so that the rules of the final report and of the live check are the same.
    /// </summary>
    public interface ISpecEvaluationService
        {
        /// <summary>
        /// Evaluates the device JSON (as saved by the application; measurements may be incomplete).
        /// </summary>
        /// <param name="deviceJson">Device data in the same JSON format as the saved files.</param>
        /// <param name="cancellationToken">Token that abandons the request.</param>
        /// <returns>The outcome; never throws except for cancellation.</returns>
        Task<SpecEvaluationOutcome> EvaluateAsync(string deviceJson, CancellationToken cancellationToken);
        }
    }
