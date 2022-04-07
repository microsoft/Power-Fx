﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerFx.Core
{
    public class FormulaTypeSchema
    {
        public enum ParamType
        {
            Number,
            String,
            Boolean,
            Date,
            Time,
            DateTime,
            DateTimeNoTimeZone,
            Color,
            Guid,
            Record,
            Table,
            Blank,
            Hyperlink,
            OptionSetValue,
            UntypedObject,
            EntityRecord,
            EntityTable,
        }

        /// <summary>
        /// Represents the type of this item. For some complex types, additional optional data is required.
        /// </summary>
        public ParamType Type { get; set; }

        /// <summary>
        /// Optional. For Records and Tables, contains the list of fields.
        /// </summary>
        public Dictionary<string, FormulaTypeSchema> Fields { get; set; }
        
        /// <summary>
        /// Optional. Used for external schema definitions and input validation.
        /// </summary>
        public bool? Required { get; set; }

        /// <summary>
        /// Optional. For entities, specifies the table logical name.
        /// </summary>
        public string TableLogicalName { get; set; }

        /// <summary>
        /// Optional. For Option Set Values, specifies the option set logical name.
        /// </summary>
        public string OptionSetName { get; set; }
    }
}
