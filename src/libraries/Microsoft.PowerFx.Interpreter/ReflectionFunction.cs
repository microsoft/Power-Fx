// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
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
            : base(DPath.Root, name, name, SG("Custom func " + name), FunctionCategories.MathAndStat, returnType, 0,
                  paramTypes.Length, paramTypes.Length, paramTypes)
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

    /// <summary>
    /// Base class for importing a C# function into Power Fx. 
    /// Dervied class should follow this convention:
    /// - class name should folow this convention: "[Method Name]" + "Function"
    /// - it should have a public static  method 'Execute'. this class will reflect over that signature to import to power fx. 
    /// </summary>
    public abstract class ReflectionFunction
    {
        private FunctionDescr _info;

        /// <summary>
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
                name = name,
                retType = returnType,
                paramTypes = paramTypes,
                _method = m
            };

        }

        class FunctionDescr
        {
            public FormulaType retType;
            public FormulaType[] paramTypes;
            public string name;

            public MethodInfo _method;
        }

        private FunctionDescr Scan()
        {
            if (_info == null)
            {
                var info = new FunctionDescr();

                var t = GetType();

                var suffix = "Function";
                info.name = t.Name.Substring(0, t.Name.Length - suffix.Length);

                var m = t.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (m == null)
                {
                    throw new InvalidOperationException($"Missing Execute method");
                }

                info.retType = GetType(m.ReturnType);
                info.paramTypes = Array.ConvertAll(m.GetParameters(), p => GetType(p.ParameterType));
                info._method = m;

                _info = info;
            }

            return _info;
        }

        static FormulaType GetType(Type t)
        {
            if (t == typeof(NumberValue))
            {
                return FormulaType.Number;
            }

            throw new NotImplementedException($"Marshal type {t.Name}");
        }


        internal TexlFunction GetTexlFunction()
        {
            var info = Scan();
            return new CustomTexlFunction(info.name, info.retType, info.paramTypes)
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
}
