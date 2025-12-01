using System;
using System.Linq;
using System.Runtime.CompilerServices;
using WPF_LCD_Test.Interfaces;

namespace WPF_LCD_Test.Models
    {
    public class LogHandler
        {
        private readonly ILocalizationService _localizationService;
        private readonly Action<string> _logAction;

        public LogHandler(ILocalizationService localizationService, Action<string> logAction)
            {
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));
            }

        // "args" is now an explicit array. This prevents the compiler from mapping a string argument to "resourceName".
        public void Log(string messageOrKey, object[]? args = null, [CallerArgumentExpression("messageOrKey")] string? resourceName = null)
            {
            try
                {
                // 1. Resolve Template
                string formatTemplate = ResolveTemplate(messageOrKey, resourceName);

                // 2. Format
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

                // 3. Output
                _logAction(finalMessage);
                }
            catch (Exception ex)
                {
                _logAction($"[Log Error] {messageOrKey} | {ex.Message}");
                }
            }

        private string ResolveTemplate(string originalValue, string? expression)
            {
            bool isLikelyResourceKey = !string.IsNullOrEmpty(expression)
                                       && !expression.Contains("\"")
                                       && !expression.Contains("+");

            if (isLikelyResourceKey)
                {
                var key = expression!.Split('.').Last().Trim();
                var translated = _localizationService.GetString(key);

                if (!translated.StartsWith("!") && !translated.EndsWith("!"))
                    {
                    return translated;
                    }
                }

            return originalValue;
            }
        }
    }