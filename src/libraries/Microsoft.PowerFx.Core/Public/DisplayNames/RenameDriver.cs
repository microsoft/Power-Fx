// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Glue;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Core
{
    public sealed class RenameDriver
    {
        private readonly RecordType _baseParameters;
        private readonly RecordType _renameParameters;
        private readonly INameResolver _resolver;
        private readonly Engine _engine;
        private readonly IBinderGlue _binderGlue;

        private Dictionary<AggregateType, WrappedDerivedRecordType> _wrappedLazyRecordTypes;

        internal RenameDriver(RecordType parameters, DPath pathToRename, DName updatedName, Engine engine, INameResolver resolver, IBinderGlue binderGlue)
        {
            var segments = new Queue<DName>(pathToRename.Segments());
            Contracts.CheckParam(segments.Count > 0, nameof(parameters));

            _baseParameters = parameters;
            _wrappedLazyRecordTypes = new Dictionary<AggregateType, WrappedDerivedRecordType>();

            // After this point, _renameParameters should have at most one logical->display pair that can change in this conversion
            _renameParameters = RenameFormulaTypeHelper(parameters, segments, updatedName) as RecordType;
            _resolver = resolver;
            _engine = engine;
            _binderGlue = binderGlue;
        }

        /// <summary>
        /// Applies rename operation to <paramref name="expressionText"/>.
        /// </summary>
        /// <param name="expressionText">Expression in which to rename the parameter field.</param>
        /// <returns>Expression with rename applied, in invariant locale.</returns>
        public string ApplyRename(string expressionText)
        {
            // Ensure expression is converted to invariant before applying rename.
            var invariantExpression = _engine.GetInvariantExpression(expressionText, _baseParameters);
            var converted = ExpressionLocalizationHelper.ConvertExpression(invariantExpression, _renameParameters, BindingConfig.Default, _resolver, _binderGlue, CultureInfo.InvariantCulture, true);

            // Convert back to the invariant expression. All parameter values are already invariant at this point, so we pass _renameParameters, but stripped of it's DisplayNameProvider.
            // Reset the wrapped cache first to ensure we clear all display name providers
            _wrappedLazyRecordTypes = new Dictionary<AggregateType, WrappedDerivedRecordType>();
            var strippedRenameParameters = GetWrappedAggregateType(_renameParameters, DisabledDisplayNameProvider.Instance) as RecordType;
            return ExpressionLocalizationHelper.ConvertExpression(converted, strippedRenameParameters, BindingConfig.Default, _resolver, _binderGlue, CultureInfo.InvariantCulture, false);
        }

        private FormulaType RenameFormulaTypeHelper(AggregateType nestedType, Queue<DName> segments, DName updatedName)
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

                return GetWrappedAggregateType(nestedType, newProvider);
            }

            if (!nestedType.TryGetFieldType(field, out var fieldType) || fieldType is not AggregateType aggregateType)
            {
                // Path doesn't exist within parameters, return as is, stripping displaynameproviders
                return GetWrappedAggregateType(nestedType, DisabledDisplayNameProvider.Instance);
            }

            var innerUpdatedType = RenameFormulaTypeHelper(aggregateType, segments, updatedName);

            // Wrap the nestedType, swapping one field for the updated one and disabling all other display names
            return GetWrappedAggregateType(nestedType, DisabledDisplayNameProvider.Instance, field, innerUpdatedType);
        }

        private AggregateType GetWrappedAggregateType(AggregateType rootType, DisplayNameProvider displayNameProvider, string replacedFieldName = null, FormulaType replacedFieldType = null)
        {
            if (!rootType._type.IsLazyType)
            {
                // Non-lazy types don't get cached
                var wrappedNonLazy = new WrappedDerivedRecordType(rootType, displayNameProvider, this, replacedFieldName, replacedFieldType);
                return rootType is TableType ? wrappedNonLazy.ToTable() : wrappedNonLazy;
            }

            var backingType = rootType._type.LazyTypeProvider.BackingFormulaType;

            // Check the cache
            if (_wrappedLazyRecordTypes.TryGetValue(backingType, out var existingWrappedType))
            {
                return rootType is TableType ? existingWrappedType.ToTable() : existingWrappedType;
            }

            // Wrap the current type and add to cache
            var wrapped = new WrappedDerivedRecordType(backingType, displayNameProvider, this, replacedFieldName, replacedFieldType);
            _wrappedLazyRecordTypes.Add(backingType, wrapped);

            return rootType is TableType ? wrapped.ToTable() : wrapped;
        }

        private class WrappedDerivedRecordType : RecordType
        {
            private readonly AggregateType _from;
            private readonly RenameDriver _renameDriver;
            private readonly string _replacedField;
            private readonly FormulaType _replacedFieldType;

            public override IEnumerable<string> FieldNames => _from.FieldNames; 

            public WrappedDerivedRecordType(AggregateType type, DisplayNameProvider provider, RenameDriver renamer, string replacedFieldName = null, FormulaType replacedFieldType = null)
                : base()
            {
                _from = type;
                _renameDriver = renamer;
                _type = DType.ReplaceDisplayNameProvider(_type, provider);
                _replacedField = replacedFieldName;
                _replacedFieldType = replacedFieldType;
            }

            public override bool TryGetFieldType(string name, out FormulaType type)
            {
                if (name == _replacedField)
                {
                    type = _replacedFieldType;
                    return true;
                }

                if (!_from.TryGetFieldType(name, out type))
                {
                    return false;
                }

                if (type is AggregateType aggregateType)
                {
                    // Ensures that as we traverse the type tree, if we encounter another lazy type, if it was already in the cache, we reuse the cached instance.
                    // Allows us to be lazy about changing DisplayNameProviders
                    type = _renameDriver.GetWrappedAggregateType(aggregateType, DisabledDisplayNameProvider.Instance);
                    return true;
                }

                return true;
            }

            public override bool Equals(object other)
            {
                return other is WrappedDerivedRecordType wrapped && _from.Equals(wrapped._from);
            }

            public override int GetHashCode()
            {
                return _from.GetHashCode();
            }
        }
    }
}
