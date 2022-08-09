// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Internal adapter for adding custom functions. 
    /// </summary>
    internal class CustomTexlFunction : TexlFunction
    {
        public Func<FormulaValue[], FormulaValue> _impl;

        public override bool SupportsParamCoercion => true;

        public CustomTexlFunction(string name, FormulaType returnType, params FormulaType[] paramTypes)
            : this(name, returnType._type, Array.ConvertAll(paramTypes, x => x._type))
        {
        }

        public CustomTexlFunction(string name, DType returnType, params DType[] paramTypes)
            : base(DPath.Root, name, name, SG("Custom func " + name), FunctionCategories.MathAndStat, returnType, 0, paramTypes.Length, paramTypes.Length, paramTypes)
        {
        }

        public override bool IsSelfContained => true;

        public static StringGetter SG(string text)
        {
            return (string locale) => text;
        }

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { SG("Arg 1") };
        }

        public virtual FormulaValue Invoke(FormulaValue[] args)
        {
            return _impl(args);
        }
    }

    // Helper for SetPropertyFunction 
    // Binds as: 
    //    SetProperty(control.property:, arg:any)
    // Invokes as:
    //    SetProperty(control, "property", arg)
    internal sealed class CustomSetPropertyFunction : TexlFunction
    {
        public override bool IsAsync => true;

        public override bool IsSelfContained => false; // marks as behavior 

        public override bool SupportsParamCoercion => false;

        public Func<FormulaValue[], FormulaValue> _impl;

        public CustomSetPropertyFunction(string name)
            : base(DPath.Root, name, name, SG(name), FunctionCategories.Behavior, DType.Boolean, 0, 2, 2)
        {
        }

        private static StringGetter SG(string text) => CustomTexlFunction.SG(text);

        public override IEnumerable<TexlStrings.StringGetter[]> GetSignatures()
        {
            yield return new[] { SG("Arg 1"), SG("Arg 2") };
        }

        // 2nd argument should be same type as 1st argument. 
        public override bool CheckInvocation(TexlBinding binding, TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
            Contracts.AssertValue(binding);
            Contracts.AssertValue(args);
            Contracts.AssertAllValues(args);
            Contracts.AssertValue(argTypes);
            Contracts.AssertAllValid(argTypes);
            Contracts.Assert(args.Length == argTypes.Length);
            Contracts.AssertValue(errors);
            Contracts.Assert(MinArity <= args.Length && args.Length <= MaxArity);

            nodeToCoercedTypeMap = null;
            returnType = DType.Boolean;

            var arg0 = argTypes[0];

            var dottedName = args[0].AsDottedName();

            // Global-scoped variable name should be a firstName.
            if (dottedName == null)
            {
                errors.EnsureError(DocumentErrorSeverity.Severe, args[0], TexlStrings.ErrNeedValidVariableName_Arg, Name, args[0]);
                return false;
            }

            var arg1 = argTypes[1];

            if (!arg0.Accepts(arg1))
            {
                errors.EnsureError(DocumentErrorSeverity.Critical, args[1], ErrBadType);
                return false;
            }

            return true;
        }

        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellation)
        {
            var result = _impl(args);
            return result;
        }
    }

    /// <summary>
    /// Base class for importing a C# function into Power Fx. 
    /// Dervied class should follow this convention:
    /// - class name should folow this convention: "[Method Name]" + "Function"
    /// - it should have a public static  method 'Execute'. this class will reflect over that signature to import to power fx. 
    /// </summary>
    public abstract class ReflectionFunction
    {
        private FunctionDescr _info;

        // Using this name opts into special SetProperty binding. 
        // This also gives us a symbol to track if we remove the special casing. 
        public const string SetPropertyName = "SetProperty";

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionFunction"/> class.
        /// Assume by defaults. Will reflect to get primitive types.
        /// </summary>
        protected ReflectionFunction()
        {
            _info = null;
        }

        // Explicitly provide types.
        // Necessary for Tables/Records
        protected ReflectionFunction(string name, FormulaType returnType, params FormulaType[] paramTypes)
        {
            var t = GetType();
            var m = t.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (m == null)
            {
                throw new InvalidOperationException($"Missing Execute method");
            }

            _info = new FunctionDescr
            {
                Name = name,
                RetType = returnType,
                ParamTypes = paramTypes,
                _method = m
            };
        }

        private class FunctionDescr
        {
            public FormulaType RetType;
            public FormulaType[] ParamTypes;
            public string Name;

            public MethodInfo _method;
        }

        private FunctionDescr Scan()
        {
            if (_info == null)
            {
                var info = new FunctionDescr();

                var t = GetType();

                var suffix = "Function";
                info.Name = t.Name.Substring(0, t.Name.Length - suffix.Length);

                var m = t.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (m == null)
                {
                    throw new InvalidOperationException($"Missing Execute method");
                }

                info.RetType = GetType(m.ReturnType);
                info.ParamTypes = Array.ConvertAll(m.GetParameters(), p => GetType(p.ParameterType));
                info._method = m;

                _info = info;
            }

            return _info;
        }

        private static FormulaType GetType(Type t)
        {
            // Handle any FormulaType deriving from Primitive<T>
            var tBase = t.BaseType;
            if (Utility.TryGetElementType(tBase, typeof(PrimitiveValue<>), out var typeArg))
            {
                if (PrimitiveValueConversions.TryGetFormulaType(typeArg, out var formulaType))
                {
                    return formulaType;
                }
            }

            throw new NotImplementedException($"Marshal type {t.Name}");
        }

        internal TexlFunction GetTexlFunction()
        {
            var info = Scan();

            // Special case SetProperty. Use reference equality to opt into special casing.
            if (object.ReferenceEquals(info.Name, SetPropertyName))
            {
                return new CustomSetPropertyFunction(info.Name)
                {
                    _impl = args => Invoke(args)
                };
            }

            return new CustomTexlFunction(info.Name, info.RetType, info.ParamTypes)
            {
                _impl = (args) => Invoke(args)
            };
        }

        public FormulaValue Invoke(FormulaValue[] args)
        {
            Scan();
            var result = _info._method.Invoke(this, args);

            return (FormulaValue)result;
        }
    }

    // A function capable of async invokes. 
    internal interface IAsyncTexlFunction
    {
        DPath Namespace { get; }

        string Name { get; }
      
        Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancel);
    }
}
