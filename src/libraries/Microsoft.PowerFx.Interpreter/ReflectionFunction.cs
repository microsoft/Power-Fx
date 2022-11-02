// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.App.ErrorContainers;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
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
        public Func<IServiceProvider, FormulaValue[], CancellationToken, Task<FormulaValue>> _impl;

        internal BigInteger LamdaParamMask;

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

        public virtual Task<FormulaValue> InvokeAsync(IServiceProvider serviceProvider, FormulaValue[] args, CancellationToken cancellationToken)
        {
            return _impl(serviceProvider, args, cancellationToken);
        }

        public override bool IsLazyEvalParam(int index)
        {
            return LamdaParamMask.TestBit(index);
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

        public override bool CheckTypesAndSemanticsOnly => true;

        public Func<FormulaValue[], Task<FormulaValue>> _impl;

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
        protected override bool CheckTypes(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
        {
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

        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken)
        {
            var result = _impl(args);
            return await result;
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

        // If non-null, specify what type of runtime config this function can accept. 
        protected Type ConfigType { get; init; }

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

            // User-facing parameter types. 
            public FormulaType[] ParamTypes;
            public string Name;

            // If not null, then arg0 is from RuntimeConfig
            public Type _configType;

            public MethodInfo _method;

            public bool _isAsync;

            public BigInteger LamdaParamMask;
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

                var paramTypes = new List<FormulaType>();

                info._isAsync = m.ReturnType.BaseType == typeof(Task);

                var parameters = m.GetParameters();
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (i == parameters.Length - 1 && info._isAsync)
                    {
                        if (parameters[i].ParameterType != typeof(CancellationToken))
                        {
                            throw new InvalidOperationException($"Last argument must be a cancellation token.");
                        }
                    }
                    else if (typeof(FormulaValue).IsAssignableFrom(parameters[i].ParameterType))
                    {
                        paramTypes.Add(GetType(parameters[i].ParameterType));
                    }
                    else if (parameters[i].ParameterType == ConfigType)
                    {
                        // Not a Formulatype, pull from RuntimeConfig
                        info._configType = parameters[i].ParameterType;
                    }
                    else if (parameters[i].ParameterType == typeof(CancellationToken) && info._isAsync)
                    {
                        throw new InvalidOperationException($"Cancellation token must be the last argument.");
                    }
                    else if (parameters[i].ParameterType == typeof(Func<Task<BooleanValue>>))
                    {
                        info.LamdaParamMask = info.LamdaParamMask | BigInteger.One << i;
                        paramTypes.Add(FormulaType.Boolean);
                    }
                    else if (parameters[i].ParameterType.BaseType == typeof(MulticastDelegate))
                    {
                        // Currently only Func<Task<BooleanValue> is supported.
                        throw new InvalidOperationException($"Unknown parameter type: {parameters[i].Name}, {parameters[i].ParameterType}. Only {typeof(Func<Task<BooleanValue>>)} is supported");
                    }
                    else
                    { 
                        // Unknown parameter type
                        throw new InvalidOperationException($"Unknown parameter type: {parameters[i].Name}, {parameters[i].ParameterType}");
                    }
                }

                info.ParamTypes = paramTypes.ToArray();
                info._method = m;

                _info = info;
            }

            return _info;
        }

        private static FormulaType GetType(Type t)
        {
            // Handle any FormulaType deriving from Primitive<T>
            var tBase = t.BaseType;

            if (tBase == typeof(Task))
            {
                tBase = t.GenericTypeArguments[0].BaseType;
            }

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
                    _impl = args => InvokeAsync(null, args, CancellationToken.None)
                };
            }

            return new CustomTexlFunction(info.Name, info.RetType, info.ParamTypes)
            {
                _impl = (runtimeConfig, args, cancellationToken) => InvokeAsync(runtimeConfig, args, cancellationToken),
                LamdaParamMask = info.LamdaParamMask,
            };
        }

        public FormulaValue Invoke(IServiceProvider serviceProvider, FormulaValue[] args)
        {
            return InvokeAsync(serviceProvider, args, CancellationToken.None).Result;
        }

        public async Task<FormulaValue> InvokeAsync(IServiceProvider serviceProvider, FormulaValue[] args, CancellationToken cancellationToken)
        {
            Scan();

            var args2 = new List<object>();
            if (ConfigType != null)
            {
                var service = serviceProvider.GetService(ConfigType);
                if (service != null)
                {
                    args2.Add(service);
                }
                else
                {
                    throw new InvalidOperationException($"Call to {_info.Name} is missing config type {_info._configType.FullName}");
                }
            }

            List<ErrorValue> errors = null;
            for (var i = 0; i < args.Length; i++)
            {
                object arg = args[i];

                // In case, ReflectionFunction was created using the constructor which takes paramtypes as optional argument paramtypes could be null.
                var expectedType = _info.ParamTypes.Length <= i ? default : _info.ParamTypes[i];
                if (arg is ErrorValue ev)
                {
                    if (errors == null)
                    {
                        errors = new List<ErrorValue>();
                    }

                    errors.Add(ev);
                }
                else if (arg is BlankValue && expectedType is NumberType)
                {
                    arg = FormulaValue.New(0);
                }
                else if (arg is BlankValue && expectedType is StringType)
                {
                    arg = FormulaValue.New(string.Empty);
                }
                else if (arg is LambdaFormulaValue lambda)
                {
                    arg = async () => (BooleanValue)await lambda.EvalAsync();
                }

                args2.Add(arg);
            }

            if (errors != null)
            {
                return ErrorValue.Combine(IRContext.NotInSource(_info.RetType), errors);
            }

            if (errors != null)
            {
                return ErrorValue.Combine(IRContext.NotInSource(FormulaType.BindingError), errors);
            }

            if (_info._isAsync)
            {
                args2.Add(cancellationToken);
            }

            var result = _info._method.Invoke(this, args2.ToArray());

            if (_info._isAsync)
            {
                var resultType = result.GetType().GenericTypeArguments[0];
                var formulaValueResult = await Unwrap(result, resultType);
                return formulaValueResult;
            }

            return (FormulaValue)result;
        }

        private static async Task<FormulaValue> Unwrap(object obj, Type resultType)
        {
            var t1 = typeof(Helper<>).MakeGenericType(resultType);
            var helper = Activator.CreateInstance(t1);
            var t2 = (Helper)helper;

            FormulaValue result = await t2.Unwrap(obj);

            return result;
        }

        private abstract class Helper
        {
            // where obj is Task<T>, T is FormulaValue 
            public abstract Task<FormulaValue> Unwrap(object obj);
        }

        private class Helper<T> : Helper
            where T : FormulaValue
        {
            // where obj is Task<T>, T is FormulaValue 
            public override async Task<FormulaValue> Unwrap(object obj)
            {
                var t = (Task<T>)obj;
                var result = await t;
                return result;
            }
        }
    }

    // A function capable of async invokes. 
    internal interface IAsyncTexlFunction
    {
        Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancellationToken);
    }
}
