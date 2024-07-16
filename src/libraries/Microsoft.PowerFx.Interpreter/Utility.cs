// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
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

        public static FormulaValue SetUntypedObject(IRContext context, UntypedObjectBase untypedObject, FormulaValue property, FormulaValue value)
        {
            try
            {
                untypedObject.SetProperty(property, value);
                return context.ResultType._type.Kind == DKind.Boolean ? FormulaValue.New(true) : FormulaValue.NewVoid();
            }
            catch (CustomFunctionErrorException ex)
            {
                return new ErrorValue(context, new ExpressionError() { Message = ex.Message, Span = context.SourceContext, Kind = ex.ErrorKind });
            }
            catch (NotImplementedException)
            {
                return CommonErrors.NotYetImplementedError(context, $"Class {untypedObject.GetType()} does not implement 'SetProperty'.");
            }
        }
    }
}
