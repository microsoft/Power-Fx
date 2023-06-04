// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Base class for importing a C# function into Power Fx. 
    /// Dervied class should follow this convention:
    /// - class name should folow this convention: "[Method Name]" + "Function" + optional postfix for function orevloading
    /// - it should have a public static  method 'Execute'. this class will reflect over that signature to import to Power Fx. 
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
            var m = t.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance) ?? throw new InvalidOperationException($"Missing Execute method");

            if (returnType._type.IsDeferred || paramTypes.Any(type => type._type.IsDeferred))
            {
                throw new NotSupportedException();
            }

            var isAsync = m.ReturnType.BaseType == typeof(Task);

            var configType = ConfigType ?? default;

            _info = new FunctionDescr(name, m, returnType, paramTypes, configType, BigInteger.Zero, isAsync);
        }

        private FunctionDescr Scan()
        {
            if (_info == null)
            {
                var t = GetType();
                var suffix = "Function";
                var name = t.Name.Substring(0, t.Name.IndexOf(suffix, StringComparison.InvariantCulture));
                var m = t.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance) ?? throw new InvalidOperationException($"Missing Execute method");
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
                    Func<Task<BooleanValue>> argLambda = async () => (BooleanValue)await lambda.EvalAsync().ConfigureAwait(false);
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

            object result = default;
            try
            {
                result = _info.Method.Invoke(this, args2.ToArray());
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException is CustomFunctionErrorException customFunctionErrorException)
                {
                    return CommonErrors.CustomError(IRContext.NotInSource(_info.RetType), customFunctionErrorException.Message);
                }

                throw e;
            }

            if (_info.IsAsync)
            {
                var resultType = result.GetType().GenericTypeArguments[0];
                try
                {
                    result = await Unwrap(result, resultType).ConfigureAwait(false);
                }
                catch (CustomFunctionErrorException customFunctionErrorException)
                {
                    return CommonErrors.CustomError(IRContext.NotInSource(_info.RetType), customFunctionErrorException.Message);
                }
            }

            var formulaResult = (FormulaValue)result;

            formulaResult ??= FormulaValue.NewBlank(_info.RetType);

            var dataSourceType = formulaResult.Type._type;
            var retType = _info.RetType._type;
            
            if (!dataSourceType.Accepts(retType, exact: true, useLegacyDateTimeAccepts: false, usePowerFxV1CompatibilityRules: true))
            {
                bool isVaid = false;

                if (retType.IsRecord)
                {
                    isVaid = true;

                    // Check if all names in dataSourceType exist in retType
                    foreach (var typedName in dataSourceType.GetNames(DPath.Root))
                    {
                        if (!retType.TryGetType(typedName.Name, out DType dsNameType))
                        {
                            isVaid = false;
                            continue;
                        }
                    }

                    // Check if dataSourceType can coerce to retType
                    if (isVaid && !dataSourceType.CoercesTo(retType, aggregateCoercion: false, isTopLevelCoercion: false, usePowerFxV1CompatibilityRules: true))
                    {
                        isVaid = false;
                    }
                }
                
                if (!isVaid)
                {
                    return CommonErrors.CustomError(formulaResult.IRContext, string.Format(CultureInfo.InvariantCulture, "Return type should have been {0}, found {1}", _info.RetType._type, formulaResult.Type._type));
                }
            }

            return formulaResult;
        }

        private static async Task<FormulaValue> Unwrap(object obj, Type resultType)
        {
            var t1 = typeof(Helper<>).MakeGenericType(resultType);
            var helper = Activator.CreateInstance(t1);
            var t2 = (Helper)helper;

            FormulaValue result = await t2.Unwrap(obj).ConfigureAwait(false);

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
                var result = await t.ConfigureAwait(false);
                return result;
            }
        }
    }
}
