// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Error message. This could be a compile time error from parsing or binding, 
    /// or it could be a runtime error wrapped in a <see cref="ErrorValue"/>.
    /// </summary>
    public class ExpressionError
    {
        public ExpressionError()
        {
        }

        /// <summary>
        /// A description of the error message. 
        /// </summary>
        public string Message
        {
            get
            {
                if (_message == null && this.MessageKey != null)
                {
                    _message = GetFormattedMessage(_messageLocale);
                }

                return _message;
            }

            // If this is set directly, it will skip localization. 
            set => _message = value;
        }

        /// <summary>
        /// Optional - provide file context for where this expression is from. 
        /// </summary>
        public FileLocation FragmentLocation { get; set; }

        /// <summary>
        /// Source location for this error within a single expression.
        /// </summary>
        public Span Span { get; set; }

        /// <summary>
        /// Runtime error code.This may be empty for compile-time errors. 
        /// </summary>
        public ErrorKind Kind { get; set; }

        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Severe;

        public ErrorResourceKey ResourceKey { get; set; }

        public string MessageKey => ResourceKey.Key;

        internal IExternalStringResources ResourceManager => ResourceKey.ResourceManager;

        public object[] MessageArgs
        {
            get => _messageArgs;
            set => _messageArgs = value;
        }

        /// <summary>
        /// A warning does not prevent executing the error. See <see cref="Severity"/> for more details.
        /// </summary>
        public bool IsWarning => Severity < ErrorSeverity.Severe;

        // localize message lazily 
        private string _message;
        internal object[] _messageArgs;
        private CultureInfo _messageLocale;

        internal CultureInfo MessageLocale => _messageLocale;

        /// <summary>
        /// Get a copy of this error message for the given locale. 
        /// <see cref="Message"/> will get lazily localized using <see cref="MessageKey"/>.
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        public ExpressionError GetInLocale(CultureInfo culture)
        {
            // In order to localize, we need a message key
            if (this.MessageKey != null)
            {
                var error = new ExpressionError
                {
                    Span = this.Span,
                    Kind = this.Kind,
                    Severity = this.Severity,
                    ResourceKey = this.ResourceKey,

                    // New message can be localized
                    _message = null, // will be lazily computed in new locale 
                    _messageArgs = this._messageArgs,
                    _messageLocale = culture
                };

                return error;
            }

            return this;
        }

        private string GetFormattedMessage(CultureInfo locale)
        {
            if (this.ResourceManager != null)
            {
                (var shortMessage, var _) = ErrorUtils.GetLocalizedErrorContent(new ErrorResourceKey(this.MessageKey, this.ResourceManager), locale, out _);
                return ErrorUtils.FormatMessage(shortMessage, _messageLocale, _messageArgs);
            }
            else
            {
                return _message;
            }
        }

        /// <summary>
        /// Get error message in the given locale.
        /// </summary>
        /// <param name="culture">CultureInfo object.</param>
        /// <param name="includeSpanDetails">If true, get error message with span details.</param>
        /// <returns></returns>
        public string GetMessageInLocale(CultureInfo culture, bool includeSpanDetails = false)
        {
            if (includeSpanDetails)
            {
                return IncludeSpanDetails(GetFormattedMessage(culture));
            }
            else
            {
                return GetFormattedMessage(culture);
            }
        }

        /// <summary>
        /// Format error message with span details.
        /// </summary>
        /// <param name="message">Message to get formatted.</param>
        /// <returns></returns>
        private string IncludeSpanDetails(string message)
        {
            var prefix = IsWarning ? "Warning" : "Error";
            if (Span != null)
            {
                return $"{prefix} {Span.Min}-{Span.Lim}: {message}";
            }
            else
            {
                return $"{prefix}: {message}";
            }
        }

        public override string ToString()
        {
            return IncludeSpanDetails(Message);
        }

        // Build the public object from an internal error object. 
        internal static ExpressionError New(IDocumentError error)
        {
            return new ExpressionError
            {
                _message = error.ShortMessage,
                _messageArgs = error.MessageArgs,
                ResourceKey = new ErrorResourceKey(error.MessageKey, error.ResourceManager),                
                Span = error.TextSpan,
                Severity = (ErrorSeverity)error.Severity
            };
        }

        internal static ExpressionError New(IDocumentError error, CultureInfo locale)
        {
            return new ExpressionError
            {
                _messageLocale = locale,
                _messageArgs = error.MessageArgs,
                ResourceKey = new ErrorResourceKey(error.MessageKey, error.ResourceManager),                
                Span = error.TextSpan,
                Severity = (ErrorSeverity)error.Severity
            };
        }

        internal static IEnumerable<ExpressionError> New(IEnumerable<IDocumentError> errors)
        {
            if (errors == null)
            {
                return Array.Empty<ExpressionError>();
            }
            else
            {
                return errors.Select(x => ExpressionError.New(x, CultureInfo.InvariantCulture)).ToArray();
            }
        }

        internal static IEnumerable<ExpressionError> New(IEnumerable<IDocumentError> errors, CultureInfo locale)
        {
            if (errors == null)
            {
                return Array.Empty<ExpressionError>();
            }
            else
            {
                return errors.Select(x => ExpressionError.New(x, locale)).ToArray();
            }
        }

        // Translate Span in original text (start,end)  to something more useful for a file. 
        internal static IEnumerable<ExpressionError> NewFragment(IEnumerable<IDocumentError> errors, string originalText, FileLocation fragmentLocation)
        {
            if (errors == null)
            {
                return Array.Empty<ExpressionError>();
            }
            else
            {
                return errors.Select(x =>
                {
                    var error = ExpressionError.New(x, null);
                    error.FragmentLocation = fragmentLocation.Apply(originalText, error.Span);
                    return error;
                }).ToArray();
            }
        }
    }

    /// <summary>
    /// Used to compare CheckResult.Errors and avoid duplicates.
    /// </summary>
    internal class ExpressionErrorComparer : EqualityComparer<ExpressionError>
    {
        // We compare only Message
        public override bool Equals(ExpressionError error1, ExpressionError error2)
        {
            if (error1 == null && error2 == null)
            {
                return true;
            }

            if (error1 == null || error2 == null)
            {
                return false;
            }

            return error1.ToString() == error2.ToString();
        }

        public override int GetHashCode(ExpressionError error)
        {
            return error.ToString().GetHashCode();
        }
    }
}
