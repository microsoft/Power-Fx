// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    public sealed class RenameDriver
    {
        private readonly RecordType _parameters;
        private readonly INameResolver _resolver;
        private readonly IBinderGlue _binder;

        internal RenameDriver(RecordType parameters, DPath pathToRename, DName updatedName, INameResolver resolver, IBinderGlue binder)
        {
            var segments = new Queue<DName>(pathToRename.Segments());
            Contracts.CheckParam(segments.Count > 0, nameof(parameters));

            // After this point, _parameters should have at most one logical->display pair that can change in this conversion
            _parameters = RenameFormulaTypeHelper(parameters, segments, updatedName) as RecordType;
            _resolver = resolver;
            _binder = binder;
        }

        /// <summary>
        /// Applies rename operation to <paramref name="expressionText"/>.
        /// </summary>
        /// <param name="expressionText">Expression in which to rename the parameter field.</param>
        /// <returns>Expression with rename applied.</returns>
        public string ApplyRename(string expressionText)
        {
            return Engine.ConvertExpression(expressionText, _parameters, _resolver, _binder, true);
        }

        private static FormulaType RenameFormulaTypeHelper(AggregateType nestedType, Queue<DName> segments, DName updatedName)
        {
            var field = segments.Dequeue();
            if (segments.Count == 0)
            {
                // Create a display name provider with only the name in question
                var names = new Dictionary<DName, DName>
                {
                    [field] = updatedName
                };
                var newProvider = new SingleSourceDisplayNameProvider(names);

                return FormulaType.Build(DType.ReplaceDisplayNameProvider(DType.DisableDisplayNameProviders(nestedType._type), newProvider));
            }

            var fieldType = nestedType.MaybeGetFieldType(field);
            if (fieldType is not AggregateType aggregateType)
            {
                // Path doesn't exist within parameters, return as is, stripping displaynameproviders
                return FormulaType.Build(DType.DisableDisplayNameProviders(nestedType._type));
            }

            var innerUpdatedType = RenameFormulaTypeHelper(aggregateType, segments, updatedName);
            var fError = false;

            // Use DType internals to swap one field type for the updated one and disable all other display names
            var dropped = DType.DisableDisplayNameProviders(nestedType._type.Drop(ref fError, DPath.Root, field));
            Contracts.Assert(!fError);

            return FormulaType.Build(dropped.Add(field, innerUpdatedType._type));
        }
    }
}
