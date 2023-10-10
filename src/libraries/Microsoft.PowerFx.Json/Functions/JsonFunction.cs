// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Entities;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types.Enums;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core.Texl.Builtins
{
    internal class JsonFunctionImpl : JsonFunction, IAsyncTexlFunction4
    {
        public Task<FormulaValue> InvokeAsync(TimeZoneInfo timezoneInfo, IRContext irContext, FormulaValue[] args, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new JsonProcessing(timezoneInfo, irContext, args).Process());
        }

        internal class JsonProcessing
        {
            private JsonFlags _flags;
            private readonly FormulaValue[] _args;
            private readonly IRContext _irContext;
            private readonly TimeZoneInfo _timeZoneInfo;

            internal JsonProcessing(TimeZoneInfo timezoneInfo, IRContext irContext, FormulaValue[] args)
            {
                _args = args;
                _irContext = irContext;
                _timeZoneInfo = timezoneInfo;
            }

            internal FormulaValue Process()
            {
                GetFlags();

                if (_flags == null || HasUnsupportedType(_args[0].Type._type, out _, out _))
                {
                    return CommonErrors.GenericInvalidArgument(_irContext);
                }

                using MemoryStream memoryStream = new MemoryStream();
                using Utf8JsonWriter writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions() { Indented = _flags.IndentFour });

                WriteToJson(writer, _args[0]);
                writer.Flush();

                string json = Encoding.UTF8.GetString(memoryStream.ToArray()).Replace(@"\u0022", @"\""").Replace(@"\u0027", @"'");

                // replace two spaces with four spaces only at the beginning of each line
                json = Regex.Replace(json, @"^(?<spc>(  )+)(?<right>[^ ].*)", @"$2$2$3", RegexOptions.Multiline);

                return new StringValue(_irContext, json);
            }

            private void GetFlags()
            {
                _flags = new JsonFlags() { HasMedia = DataHasMedia(_args[0].Type._type) };

                if (_args.Length > 1 && _args[1] is StringValue arg1string)
                {
                    _flags.IgnoreBinaryData = arg1string.Value.Contains("G");
                    _flags.IgnoreUnsupportedTypes = arg1string.Value.Contains("I");
                    _flags.IncludeBinaryData = arg1string.Value.Contains("B");
                    _flags.IndentFour = arg1string.Value.Contains("4");
                }

                if (_args.Length > 1 && _args[1] is OptionSetValue arg1optionset)
                {
                    _flags.IgnoreBinaryData = arg1optionset.Option == "IgnoreBinaryData";
                    _flags.IgnoreUnsupportedTypes = arg1optionset.Option == "IgnoreUnsupportedTypes";
                    _flags.IncludeBinaryData = arg1optionset.Option == "IncludeBinaryData";
                    _flags.IndentFour = arg1optionset.Option == "IndentFour";
                }

                if ((_flags.IncludeBinaryData && _flags.IgnoreBinaryData) ||
                    (_flags.HasMedia && !_flags.IncludeBinaryData && !_flags.IgnoreBinaryData) ||
                    (!_flags.IgnoreUnsupportedTypes && HasUnsupportedType(_args[0].Type._type, out var _, out var _)))
                {
                    _flags = null;
                    return;
                }
            }

            internal void WriteToJson(Utf8JsonWriter writer, FormulaValue arg)
            {
                if (arg is BlankValue blankValue)
                {
                    writer.WriteNullValue();
                }
                else if (arg is BooleanValue booleanValue)
                {
                    writer.WriteBooleanValue(booleanValue.Value);
                }
                else if (arg is ColorValue colorValue)
                {
                    writer.WriteStringValue(GetColorString(colorValue.Value));
                }
                else if (arg is DateTimeValue dateTimeValue)
                {
                    writer.WriteStringValue(ConvertToUTC(dateTimeValue.GetConvertedValue(_timeZoneInfo), _timeZoneInfo).ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture));
                }
                else if (arg is DateValue dateValue)
                {
                    writer.WriteStringValue(ConvertToUTC(dateValue.GetConvertedValue(_timeZoneInfo), _timeZoneInfo).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }
                else if (arg is DecimalValue decimalValue)
                {
                    writer.WriteNumberValue(decimalValue.Value);
                }
                else if (arg is GuidValue guidValue)
                {
                    writer.WriteStringValue(guidValue.Value.ToString("D", CultureInfo.InvariantCulture));
                }
                else if (arg is NumberValue numberValue)
                {
                    writer.WriteNumberValue(numberValue.Value);
                }
                else if (arg is OptionSetValue optionSetValue)
                {
                    if (optionSetValue.Type._type.OptionSetInfo is EnumSymbol enumSymbol)
                    {
                        if (enumSymbol.EntityName == "Color")
                        {
                            Color color = Color.FromArgb((int)(uint)(double)optionSetValue.ExecutionValue);
                            writer.WriteStringValue(GetColorString(color));
                        }
                        else if (IsNumeric(optionSetValue.ExecutionValue, out object number))
                        {
                            if (number is decimal dec)
                            {
                                writer.WriteNumberValue(dec);
                            }
                            else if (number is double dbl)
                            {
                                writer.WriteNumberValue(dbl);
                            }
                            else if (number is float flt)
                            {
                                writer.WriteNumberValue(flt);
                            }
                            else if (number is int i)
                            {
                                writer.WriteNumberValue(i);
                            }
                            else if (number is uint ui)
                            {
                                writer.WriteNumberValue(ui);
                            }
                            else if (number is long l)
                            {
                                writer.WriteNumberValue(l);
                            }
                            else if (number is ulong ul)
                            {
                                writer.WriteNumberValue(ul);
                            }
                        }
                        else
                        {
                            writer.WriteStringValue(optionSetValue.ExecutionValue.ToString());
                        }
                    }
                    else if (optionSetValue.Type._type.OptionSetInfo is IExternalOptionSet ops)
                    {
                        writer.WriteStringValue(optionSetValue.Option);
                    }
                }
                else if (arg is RecordValue rv)
                {
                    writer.WriteStartObject();

                    foreach (NamedValue namedValue in rv.Fields)
                    {
                        writer.WritePropertyName(namedValue.Name);
                        WriteToJson(writer, namedValue.Value);
                    }

                    writer.WriteEndObject();
                }
                else if (arg is StringValue sv)
                {
                    writer.WriteStringValue(sv.Value);
                }
                else if (arg is TableValue tv)
                {
                    writer.WriteStartArray();

                    foreach (DValue<RecordValue> row in tv.Rows)
                    {
                        WriteToJson(writer, row.Value);
                    }

                    writer.WriteEndArray();
                }
                else if (arg is TimeValue tmv)
                {
                    writer.WriteStringValue(tmv.Value.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture));
                }
                else
                {
                    throw new ArgumentException($"Unable to serialize type {arg.GetType().FullName} to Json format.");
                }
            }

            internal static string GetColorString(Color color) => $"#{color.R:x2}{color.G:x2}{color.B:x2}{color.A:x2}";

            internal static bool IsNumeric(object value, out object number)
            {
                number = value switch
                {
                    byte b => (int)b,
                    decimal d => d,
                    double dbl => dbl,
                    float flt => flt,
                    int i => i,
                    long l => l,
                    sbyte sb => (int)sb,
                    short s => (int)s,
                    uint ui => ui,
                    ulong ul => ul,
                    ushort us => (uint)us,
                    _ => null
                };

                return number != null;
            }

            private class JsonFlags
            {
                internal bool Compact => !IndentFour;

                internal bool HasMedia = false;

                internal bool IgnoreBinaryData = false;

                internal bool IgnoreUnsupportedTypes = false;

                internal bool IncludeBinaryData = false;

                internal bool IndentFour = false;
            }

            private static DateTime ConvertToUTC(DateTime dateTime, TimeZoneInfo fromTimeZone)
            {
                var resultDateTime = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), fromTimeZone.GetUtcOffset(dateTime));
                return resultDateTime.UtcDateTime;
            }
        }
    }
}
