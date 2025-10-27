﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal class JsonFunctionImpl : JsonFunction, IAsyncTexlFunction5
    {
        public Task<FormulaValue> InvokeAsync(IServiceProvider runtimeServiceProvider, FormulaType type, FormulaValue[] args, CancellationToken cancellationToken)
        {
            TimeZoneInfo timeZoneInfo = runtimeServiceProvider.GetService(typeof(TimeZoneInfo)) as TimeZoneInfo ?? throw new InvalidOperationException("TimeZoneInfo is required");
            Canceller canceller = runtimeServiceProvider.GetService(typeof(Canceller)) as Canceller ?? new Canceller(() => cancellationToken.ThrowIfCancellationRequested());

            return Task.FromResult(new JsonProcessing(timeZoneInfo, type, args, supportsLazyTypes).Process(canceller));
        }

        internal class JsonProcessing
        {
            private readonly FormulaValue[] _arguments;

            private readonly FormulaType _type;

            private readonly TimeZoneInfo _timeZoneInfo;

            private readonly bool _supportsLazyTypes;

            internal JsonProcessing(TimeZoneInfo timezoneInfo, FormulaType type, FormulaValue[] args, bool supportsLazyTypes)
            {
                _arguments = args;
                _timeZoneInfo = timezoneInfo;
                _type = type;
                _supportsLazyTypes = supportsLazyTypes;
            }

            internal FormulaValue Process(Canceller canceller)
            {
                canceller.ThrowIfCancellationRequested();

                JsonFlags flags = GetFlags();

                if (flags == null || JsonFunction.HasUnsupportedType(_arguments[0].Type._type, _supportsLazyTypes, out _, out _))
                {
                    var context = IRContext.NotInSource(_type);
                    return new ErrorValue(context, new ExpressionError()
                    {
                        Message = "Invalid Argument",
                        Span = context.SourceContext,
                        Kind = ErrorKind.InvalidArgument
                    });
                }

                using MemoryStream memoryStream = new MemoryStream();
                using Utf8JsonWriter writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions() { Indented = flags.IndentFour, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                Utf8JsonWriterVisitor jsonWriterVisitor = new Utf8JsonWriterVisitor(writer, _timeZoneInfo, flattenValueTables: flags.FlattenValueTables, canceller);

                try
                {
                    _arguments[0].Visit(jsonWriterVisitor);
                    writer.Flush();
                }
                catch (InvalidOperationException)
                {
                    if (!jsonWriterVisitor.ErrorValues.Any())
                    {
                        // Unexpected error, rethrow
                        throw;
                    }
                }

                if (jsonWriterVisitor.ErrorValues.Any())
                {
                    if (jsonWriterVisitor.ErrorValues.Count == 1)
                    {
                        return jsonWriterVisitor.ErrorValues[0];
                    }

                    return ErrorValue.Combine(IRContext.NotInSource(_type), jsonWriterVisitor.ErrorValues);
                }

                string json = Encoding.UTF8.GetString(memoryStream.ToArray());

                // replace two spaces with four spaces only at the beginning of each line
                json = Regex.Replace(json, @"^(?<spc>(  )+)(?<right>[^ ].*)", @"$2$2$3", RegexOptions.Multiline);

                return new StringValue(IRContext.NotInSource(_type), json);
            }

            private JsonFlags GetFlags()
            {
                JsonFlags flags = new JsonFlags() { HasMedia = JsonFunction.DataHasMedia(_arguments[0].Type._type) };

                if (_arguments.Length > 1)
                {
                    string optionString = null;

                    switch (_arguments[1])
                    {
                        // Can be built up through concatenation, may have more than one value
                        case OptionSetValue osv:
                            optionString = (string)osv.ExecutionValue;
                            break;

                        // StringValue returned when StronglyTypedBuiltinOptionSet is not used
                        case StringValue sv:
                            optionString = sv.Value;
                            break;

                            // if not one of these, will check optionString != null below
                    }

                    if (optionString != null)
                    {
                        flags.IgnoreBinaryData = optionString.Contains("G");
                        flags.IgnoreUnsupportedTypes = optionString.Contains("I");
                        flags.IncludeBinaryData = optionString.Contains("B");
                        flags.IndentFour = optionString.Contains("4");
                        flags.FlattenValueTables = optionString.Contains("_");
                    }
                }

                if ((flags.IncludeBinaryData && flags.IgnoreBinaryData) ||
                    (flags.HasMedia && !flags.IncludeBinaryData && !flags.IgnoreBinaryData) ||
                    (!flags.IgnoreUnsupportedTypes && JsonFunction.HasUnsupportedType(_arguments[0].Type._type, _supportsLazyTypes, out var _, out var _)))
                {
                    return null;
                }

                return flags;
            }

            private class Utf8JsonWriterVisitor : IValueVisitor
            {
                private const int _maxDepth = 20;           // maximum depth of UO

                private const int _maxLength = 1024 * 1024; // 1 MB, maximum number of bytes allowed to be sent to Utf8JsonWriter

                private readonly Utf8JsonWriter _writer;

                private readonly TimeZoneInfo _timeZoneInfo;

                private readonly bool _flattenValueTables;

                private readonly Canceller _canceller;

                internal readonly List<ErrorValue> ErrorValues = new List<ErrorValue>();

                internal Utf8JsonWriterVisitor(Utf8JsonWriter writer, TimeZoneInfo timeZoneInfo, bool flattenValueTables, Canceller canceller)
                {
                    _writer = writer;
                    _timeZoneInfo = timeZoneInfo;
                    _flattenValueTables = flattenValueTables;

                    _canceller = canceller;
                }

                private void CheckLimitsAndCancellation(int index)
                {
                    _canceller.ThrowIfCancellationRequested();

                    if (index > _maxDepth)
                    {
                        IRContext irContext = IRContext.NotInSource(FormulaType.UntypedObject);
                        ErrorValues.Add(new ErrorValue(irContext, new ExpressionError()
                        {
                            ResourceKey = TexlStrings.ErrReachedMaxJsonDepth,
                            Span = irContext.SourceContext,
                            Kind = ErrorKind.InvalidArgument
                        }));

                        throw new InvalidOperationException($"Maximum depth {_maxDepth} reached while traversing JSON payload.");
                    }

                    if (_writer.BytesCommitted + _writer.BytesPending > _maxLength)
                    {
                        IRContext irContext = IRContext.NotInSource(FormulaType.UntypedObject);
                        ErrorValues.Add(new ErrorValue(irContext, new ExpressionError()
                        {
                            ResourceKey = TexlStrings.ErrReachedMaxJsonLength,
                            Span = irContext.SourceContext,
                            Kind = ErrorKind.InvalidArgument
                        }));

                        throw new InvalidOperationException($"Maximum length {_maxLength} reached in JSON function.");
                    }
                }

                public void Visit(BlankValue blankValue)
                {
                    _writer.WriteNullValue();
                }

                public void Visit(BooleanValue booleanValue)
                {
                    _writer.WriteBooleanValue(booleanValue.Value);
                }

                public void Visit(ColorValue colorValue)
                {
                    _writer.WriteStringValue(GetColorString(colorValue.Value));
                }

                public void Visit(DateTimeValue dateTimeValue)
                {
                    _writer.WriteStringValue(ConvertToUTC(dateTimeValue.GetConvertedValue(_timeZoneInfo), _timeZoneInfo).ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture));
                }

                public void Visit(DateValue dateValue)
                {
                    _writer.WriteStringValue(dateValue.GetConvertedValue(null).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }

                public void Visit(DecimalValue decimalValue)
                {
                    _writer.WriteNumberValue(decimalValue.Value);
                }

                public void Visit(ErrorValue errorValue)
                {
                    ErrorValues.Add(errorValue);
                    _writer.WriteStringValue("ErrorValue");
                }

                public void Visit(GuidValue guidValue)
                {
                    _writer.WriteStringValue(guidValue.Value.ToString("D", CultureInfo.InvariantCulture));
                }

                public void Visit(NumberValue numberValue)
                {
                    _writer.WriteNumberValue(numberValue.Value);
                }

                public void Visit(OptionSetValue optionSetValue)
                {
                    if (optionSetValue.Type._type.OptionSetInfo is EnumSymbol enumSymbol)
                    {
                        if (enumSymbol.EntityName == "Color")
                        {
                            Color color = Color.FromArgb((int)(uint)(double)optionSetValue.ExecutionValue);
                            _writer.WriteStringValue(GetColorString(color));
                        }
                        else if (optionSetValue.ExecutionValue is byte b)
                        {
                            _writer.WriteNumberValue((int)b);
                        }
                        else if (optionSetValue.ExecutionValue is decimal d)
                        {
                            _writer.WriteNumberValue(d);
                        }
                        else if (optionSetValue.ExecutionValue is double dbl)
                        {
                            _writer.WriteNumberValue(dbl);
                        }
                        else if (optionSetValue.ExecutionValue is float flt)
                        {
                            _writer.WriteNumberValue(flt);
                        }
                        else if (optionSetValue.ExecutionValue is int i)
                        {
                            _writer.WriteNumberValue(i);
                        }
                        else if (optionSetValue.ExecutionValue is long l)
                        {
                            _writer.WriteNumberValue(l);
                        }
                        else if (optionSetValue.ExecutionValue is sbyte sb)
                        {
                            _writer.WriteNumberValue((int)sb);
                        }
                        else if (optionSetValue.ExecutionValue is short s)
                        {
                            _writer.WriteNumberValue((int)s);
                        }
                        else if (optionSetValue.ExecutionValue is uint ui)
                        {
                            _writer.WriteNumberValue(ui);
                        }
                        else if (optionSetValue.ExecutionValue is ulong ul)
                        {
                            _writer.WriteNumberValue(ul);
                        }
                        else if (optionSetValue.ExecutionValue is ushort us)
                        {
                            _writer.WriteNumberValue((uint)us);
                        }
                        else
                        {
                            _writer.WriteStringValue(optionSetValue.ExecutionValue.ToString());
                        }
                    }
                    else if (optionSetValue.Type._type.OptionSetInfo is IExternalOptionSet ops)
                    {
                        _writer.WriteStringValue(optionSetValue.Option);
                    }
                }

                public void Visit(RecordValue recordValue)
                {
                    _writer.WriteStartObject();

                    foreach (NamedValue namedValue in recordValue.Fields.OrderBy(f => f.Name, StringComparer.Ordinal))
                    {
                        CheckLimitsAndCancellation(0);

                        _writer.WritePropertyName(namedValue.Name);
                        namedValue.Value.Visit(this);
                    }

                    _writer.WriteEndObject();
                }

                public void Visit(StringValue stringValue)
                {
                    _writer.WriteStringValue(stringValue.Value);
                }

                public void Visit(TableValue tableValue)
                {
                    _writer.WriteStartArray();

                    var isSingleColumnValueTable = false;
                    if (_flattenValueTables)
                    {
                        var fieldTypes = tableValue.Type.GetFieldTypes();
                        var firstField = fieldTypes.FirstOrDefault();
                        if (firstField != null && !fieldTypes.Skip(1).Any() && firstField.Name.Value == TexlFunction.ColumnName_ValueStr)
                        {
                            isSingleColumnValueTable = true;
                        }
                    }

                    foreach (DValue<RecordValue> row in tableValue.Rows)
                    {
                        CheckLimitsAndCancellation(0);

                        if (row.IsBlank)
                        {
                            row.Blank.Visit(this);
                        }
                        else if (row.IsError)
                        {
                            row.Error.Visit(this);
                        }
                        else
                        {
                            if (isSingleColumnValueTable)
                            {
                                var namedValue = row.Value.Fields.First();
                                namedValue.Value.Visit(this);
                            }
                            else
                            {
                                row.Value.Visit(this);
                            }
                        }
                    }

                    _writer.WriteEndArray();
                }

                public void Visit(TimeValue timeValue)
                {
                    _writer.WriteStringValue(timeValue.Value.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture));
                }

                public void Visit(UntypedObjectValue untypedObjectValue)
                {
                    Visit(untypedObjectValue.Impl);
                }

                private void Visit(IUntypedObject untypedObject, int depth = 0)
                {
                    FormulaType type = untypedObject.Type;

                    CheckLimitsAndCancellation(depth);

                    if (type is StringType)
                    {
                        _writer.WriteStringValue(untypedObject.GetString());
                    }
                    else if (type is DecimalType)
                    {
                        _writer.WriteNumberValue(untypedObject.GetDecimal());
                    }
                    else if (type is NumberType)
                    {
                        _writer.WriteNumberValue(untypedObject.GetDouble());
                    }
                    else if (type is BooleanType)
                    {
                        _writer.WriteBooleanValue(untypedObject.GetBoolean());
                    }
                    else if (type is ExternalType externalType)
                    {
                        if (externalType.Kind == ExternalTypeKind.Array || externalType.Kind == ExternalTypeKind.ArrayAndObject)
                        {
                            _writer.WriteStartArray();

                            for (var i = 0; i < untypedObject.GetArrayLength(); i++)
                            {
                                CheckLimitsAndCancellation(depth);

                                IUntypedObject row = untypedObject[i];
                                Visit(row, depth + 1);
                            }

                            _writer.WriteEndArray();
                        }
                        else if ((externalType.Kind == ExternalTypeKind.Object || externalType.Kind == ExternalTypeKind.ArrayAndObject) && untypedObject.TryGetPropertyNames(out IEnumerable<string> propertyNames))
                        {
                            _writer.WriteStartObject();

                            foreach (var propertyName in propertyNames.OrderBy(prop => prop, StringComparer.Ordinal))
                            {
                                CheckLimitsAndCancellation(depth);

                                if (untypedObject.TryGetProperty(propertyName, out IUntypedObject res))
                                {
                                    _writer.WritePropertyName(propertyName);
                                    Visit(res, depth + 1);
                                }
                            }

                            _writer.WriteEndObject();
                        }
                        else if (externalType.Kind == ExternalTypeKind.UntypedNumber)
                        {
                            _writer.WriteRawValue(untypedObject.GetUntypedNumber());
                        }
                        else
                        {
                            throw new NotSupportedException("Unknown ExternalType");
                        }
                    }
                    else
                    {
                        throw new NotSupportedException("Unknown IUntypedObject");
                    }
                }

                public void Visit(BlobValue value)
                {
                    if (value.Content is Base64Blob)
                    {
                        _writer.WriteStringValue(value.GetAsBase64Async(CancellationToken.None).Result);
                    }
                    else
                    {
                        _writer.WriteBase64StringValue(value.GetAsByteArrayAsync(CancellationToken.None).Result);
                    }
                }
            }

            internal static string GetColorString(Color color) => $"#{color.R:x2}{color.G:x2}{color.B:x2}{color.A:x2}";

            private class JsonFlags
            {
                internal bool Compact => !IndentFour;

                internal bool HasMedia = false;

                internal bool IgnoreBinaryData = false;

                internal bool IgnoreUnsupportedTypes = false;

                internal bool IncludeBinaryData = false;

                internal bool IndentFour = false;

                internal bool FlattenValueTables = false;
            }

            private static DateTime ConvertToUTC(DateTime dateTime, TimeZoneInfo fromTimeZone)
            {
                var resultDateTime = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), fromTimeZone.GetUtcOffset(dateTime));
                return resultDateTime.UtcDateTime;
            }
        }
    }
}
