// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    public sealed partial class RecalcEngine
    {
        private class UDFTexlFunction : CustomTexlFunction, IAsyncTexlFunction
        {
            internal NamedFormulaType[] _parameterNames;
            internal IExpression _expr;
            internal LazyCheck _check;

            public UDFTexlFunction(string name, FormulaType returnType, params FormulaType[] paramTypes)
                : base(name, returnType, paramTypes)
            {
            }

            public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancel)
            {
                // $$$ There's a lot of unnecessary string packing overhead here 
                // because Eval wants a Record rather than a resolved arg array.                 
                var parameters = FormulaValue.NewRecordFromFields(Zip(_parameterNames, args));

                var result = await LazyGetExpression().EvalAsync(parameters, cancel);

                return result;
            }

            public IExpression LazyGetExpression()
            {
                if (_expr == null)
                {
                    var check = _check.Get();
                    check.ThrowOnErrors();
                    _expr = check.Expression;
                }

                return _expr;
            }
        }

        private class LazyCheck
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

        public void DefineFunction(string name, FormulaType retType, string body, params NamedFormulaType[] parameters)
        {
            // $$$ Would be a good helper function 
            var record = new RecordType();
            foreach (var p in parameters)
            {
                record = record.Add(p);
            }

            var check = new LazyCheck(this, body, record);

            //check.ThrowOnErrors();

            //var retType = check.ReturnType; // infer return result!

            var func = new UDFTexlFunction(name, retType, parameters.Select(x => x.Type).ToArray())
            {
                _parameterNames = parameters,
                _check = check,

                //_expr = check.Expression
            };

            _customFuncs[name] = func;
            _funcsToBind.Add(func);
        }

        public void BindAllUDFs()
        {
            foreach (var func in _funcsToBind)
            {
                func.LazyGetExpression();
            }

            _funcsToBind.Clear();
        }
    }
}
