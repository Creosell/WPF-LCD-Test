using System.Runtime.CompilerServices;
using WPF_LCD_Test.Interfaces;

namespace WPF_LCD_Test.Models
    {
    /// <summary>
    /// Handles localized logging with automatic resource key resolution.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of LogHandler.
    /// </remarks>
    /// <param name="localizationService">Service for resolving localized strings.</param>
    /// <param name="logAction">Action to invoke with formatted log message.</param>
    public class LogHandler(ILocalizationService localizationService, Action<string> logAction)
        {
        private readonly ILocalizationService _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        private readonly Action<string> _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));

        /// <summary>
        /// Logs a message with optional formatting arguments and automatic resource key resolution.
        /// </summary>
        /// <param name="messageOrKey">Message text or resource key.</param>
        /// <param name="args">Optional formatting arguments.</param>
        /// <param name="resourceName">Automatically captured resource name for key resolution.</param>
        public void Log(string messageOrKey, object[]? args = null, [CallerArgumentExpression(nameof(messageOrKey))] string? resourceName = null)
            {
            try
                {
                string formatTemplate = ResolveTemplate(messageOrKey, resourceName);

                string finalMessage;
                if (args != null && args.Length > 0)
                    {
                    try
                        {
                        finalMessage = string.Format(formatTemplate, args);
                        }
                    catch (FormatException)
                        {
                        finalMessage = $"{formatTemplate} [Args: {string.Join(", ", args)}]";
                        }
                    }
                else
                    {
                    finalMessage = formatTemplate;
                    }

                _logAction(finalMessage);
                }
            catch (Exception ex)
                {
                try
                    {
                    _logAction($"[Log Error] {messageOrKey} | {ex.Message}");
                    }
                catch
                    {
                    // Suppress exceptions in error logging to prevent infinite loops
                    System.Diagnostics.Debug.WriteLine($"[Log Error] Failed to log error: {ex.Message}");
                    }
                }
            }

        /// <summary>
        /// Resolves message template by attempting to translate resource keys.
        /// </summary>
        /// <param name="originalValue">Original message or key value.</param>
        /// <param name="expression">Caller expression for key detection.</param>
        /// <returns>Localized template if key found, otherwise original value.</returns>
        private string ResolveTemplate(string originalValue, string? expression)
            {
            bool isLikelyResourceKey = !string.IsNullOrEmpty(expression)
                                       && !expression.Contains('\"')
                                       && !expression.Contains('+');

            if (isLikelyResourceKey)
                {
                var key = expression!.Split('.').Last().Trim();
                var translated = _localizationService.GetString(key);

                if (!translated.StartsWith('!') && !translated.EndsWith('!'))
                    {
                    return translated;
                    }
                }

            return originalValue;
            }
        }
    }