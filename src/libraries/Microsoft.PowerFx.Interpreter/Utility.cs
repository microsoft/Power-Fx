// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    internal static class Utility
    {
        // Helper. Given a type Foo<T>,  extract the T when genericDef is Foo<>.
        public static bool TryGetElementType(Type type, Type genericDef, out Type elementType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericDef)
            {
                elementType = type.GenericTypeArguments[0];
                return true;
            }
            else
            {
                elementType = null;
                return false;
            }
        }

        /// <summary>
        /// Get a service from the <paramref name="serviceProvider"/>,
        /// Returns null if not present.
        /// </summary>
        public static T GetService<T>(this IServiceProvider serviceProvider)
        {
            return (T)serviceProvider.GetService(typeof(T));
        }

        public static FormulaValue SetUntypedObject(this UntypedObjectBase untypedObject, IRContext context, StringValue property, FormulaValue value, CultureInfo locale)
        {
            try
            {
                untypedObject.SetProperty(property.Value, value);
                return context.ResultType._type.Kind == DKind.Boolean ? FormulaValue.New(true) : FormulaValue.NewVoid();
            }
            catch (CustomFunctionErrorException ex)
            {
                return new ErrorValue(context, new ExpressionError() { Message = ex.Message, Span = context.SourceContext, Kind = ex.ErrorKind });
            }
            catch (NotImplementedException)
            {
                return CommonErrors.UntypedObjectDoesNotImplementSetPropertyError(context, untypedObject.GetType().ToString());
            }
        }

        public static FormulaValue SetUntypedObject(this UntypedObjectBase untypedObject, IRContext context, NumberValue property, FormulaValue value, CultureInfo locale)
        {
            try
            {
                untypedObject.SetIndex((int)property.Value, value);
                return context.ResultType._type.Kind == DKind.Boolean ? FormulaValue.New(true) : FormulaValue.NewVoid();
            }
            catch (CustomFunctionErrorException ex)
            {
                return new ErrorValue(context, new ExpressionError() { Message = ex.Message, Span = context.SourceContext, Kind = ex.ErrorKind });
            }
            catch (NotImplementedException)
            {
                return CommonErrors.UntypedObjectDoesNotImplementSetPropertyError(context, untypedObject.GetType().ToString());
            }
        }
    }
}
