// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter
{
    internal class UDFHelper
    {
        public class LazyCheck
        {
            internal string _expressionText;
            internal RecordType _parameterType;
            internal ParserOptions _options;
            internal Engine _engine;

            public LazyCheck(Engine engine, string expressionText, RecordType parameterType = null, ParserOptions options = null)
            {
                _engine = engine;
                _expressionText = expressionText;
                _parameterType = parameterType;
                _options = options;
            }

            public CheckResult Get() => _engine.Check(_expressionText, _parameterType, _options);
        }

        // would also be a good generic helper 
        private static NamedValue[] Zip(NamedFormulaType[] parameters, FormulaValue[] args)
        {
            if (parameters.Length != args.Length)
            {
                throw new ArgumentException();
            }

            var result = new NamedValue[args.Length];
            for (var i = 0; i < args.Length; i++)
            {
                result[i] = new NamedValue(parameters[i].Name, args[i]);
            }

            return result;
        }
        
        public class UDFDefinition
        {
            internal string _name;
            internal string _body;
            internal FormulaType _returnType;
            internal NamedFormulaType[] _parameters;

            public UDFDefinition(string name, string body, FormulaType returnType, params NamedFormulaType[] parameters)
            {
                _name = name;
                _body = body;
                _returnType = returnType;
                _parameters = parameters;
            }
        }

        internal class UDFLazyBinder
        {
            internal UDFTexlFunction _function;
            internal string _name;
            internal List<ExpressionError> _expressionError = new List<ExpressionError>();

            public UDFLazyBinder(UDFTexlFunction texlFunction, string name)
            { 
                _function = texlFunction;
                _name = name;
            }

            // Used when trying to define a function, there is an error before binding, such as the function name already being defined.
            public UDFLazyBinder(ExpressionError expressionError, string name)
            {
                _expressionError.Add(expressionError);
                _name = name;
            }

            public string GetName()
            {
                return _name;
            }

            public IEnumerable<ExpressionError> Bind()
            {
                if (_expressionError.Any())
                {
                    return _expressionError;
                }

                return _function.Bind();
            } 
        }

        internal class UDFTexlFunction : CustomTexlFunction, IAsyncTexlFunction
        {
            internal NamedFormulaType[] _parameterNames;
            internal IExpression _expr;
            internal LazyCheck _check;

            public override bool SupportsParamCoercion => false;

            public UDFTexlFunction(string name, FormulaType returnType, params FormulaType[] paramTypes)
                : base(name, returnType, paramTypes)
            {
            }

            public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancel)
            {
                // $$$ There's a lot of unnecessary string packing overhead here 
                // because Eval wants a Record rather than a resolved arg array.                 
                var parameters = FormulaValue.NewRecordFromFields(Zip(_parameterNames, args));

                var result = await GetExpression().EvalAsync(parameters, cancel);

                return result;
            }

            public IEnumerable<ExpressionError> Bind()
            {
                var check = _check.Get();
                if (!check.IsSuccess)
                {
                    return check.Errors;
                }

                _expr = check.Expression;
                return new List<ExpressionError>();
            }

            public IExpression GetExpression()
            {
                if (_expr == null)
                {
                    throw new UDFBindingMissingException();
                }

                return _expr;
            }
        }
    }
}
