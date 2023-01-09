// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
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
using Microsoft.PowerFx.Functions;
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
        public override bool CheckTypes(TexlNode[] args, DType[] argTypes, IErrorContainer errors, out DType returnType, out Dictionary<TexlNode, DType> nodeToCoercedTypeMap)
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

            if (returnType._type.IsDeferred || paramTypes.Any(type => type._type.IsDeferred))
            {
                throw new NotSupportedException();
            }

            var isAsync = m.ReturnType.BaseType == typeof(Task);

            var configType = ConfigType ?? default;

            _info = new FunctionDescr(name, m, returnType, paramTypes, configType, BigInteger.Zero, isAsync);
        }

        private class FunctionDescr
        {
            internal string Name { get; }

            internal MethodInfo Method { get; }

            internal FormulaType RetType { get; }

            // User-facing parameter types. 
            internal FormulaType[] ParamTypes { get; }

            // If not null, then arg0 is from RuntimeConfig
            internal Type ConfigType { get; }

            internal bool IsAsync { get; }

            internal BigInteger LamdaParamMask { get; }

            public FunctionDescr(string name, MethodInfo method, FormulaType retType, FormulaType[] paramTypes, Type configType, BigInteger lamdaParamMask, bool isAsync = false)
            {
                Name = name;
                Method = method;
                RetType = retType;
                ParamTypes = paramTypes;
                ConfigType = configType;
                IsAsync = isAsync;
                LamdaParamMask = lamdaParamMask;
            }
        }

        private FunctionDescr Scan()
        {
            if (_info == null)
            {
                var t = GetType();

                var suffix = "Function";
                var name = t.Name.Substring(0, t.Name.Length - suffix.Length);

                var m = t.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (m == null)
                {
                    throw new InvalidOperationException($"Missing Execute method");
                }

                var returnType = GetType(m.ReturnType);

                var paramTypes = new List<FormulaType>();

                var isAsync = m.ReturnType.BaseType == typeof(Task);

                var parameters = m.GetParameters();

                var configType = default(Type);

                BigInteger lamdaParamMask = default;
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (i == parameters.Length - 1 && isAsync)
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
                        configType = parameters[i].ParameterType;
                    }
                    else if (parameters[i].ParameterType == typeof(CancellationToken) && isAsync)
                    {
                        throw new InvalidOperationException($"Cancellation token must be the last argument.");
                    }
                    else if (parameters[i].ParameterType == typeof(Func<Task<BooleanValue>>))
                    {
                        lamdaParamMask = lamdaParamMask | BigInteger.One << i;
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

                _info = new FunctionDescr(name, m, returnType, paramTypes.ToArray(), configType, lamdaParamMask, isAsync);
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
                    throw new InvalidOperationException($"Call to {_info.Name} is missing config type {_info.ConfigType.FullName}");
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
                else if (arg is BlankValue)
                {
                    if (errors == null)
                    {
                        errors = new List<ErrorValue>();
                    }

                    errors.Add(CommonErrors.RuntimeTypeMismatch(IRContext.NotInSource(FormulaType.Blank)));
                }
                else if (arg is LambdaFormulaValue lambda)
                {
                    Func<Task<BooleanValue>> argLambda = async () => (BooleanValue)await lambda.EvalAsync();
                    arg = argLambda;
                }

                args2.Add(arg);
            }

            if (errors != null)
            {
                return ErrorValue.Combine(IRContext.NotInSource(_info.RetType), errors);
            }

            if (_info.IsAsync)
            {
                args2.Add(cancellationToken);
            }

            var result = _info.Method.Invoke(this, args2.ToArray());

            if (_info.IsAsync)
            {
                var resultType = result.GetType().GenericTypeArguments[0];
                result = await Unwrap(result, resultType);
            }

            var formulaResult = (FormulaValue)result;   
            
            if (formulaResult != null && formulaResult.Type != _info.RetType)
            {
                return CommonErrors.CustomError(
                    formulaResult.IRContext,
                    string.Format("Return type should have been {0}, found {1}", _info.RetType._type, formulaResult.Type._type));
            }

            return formulaResult;
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
}
