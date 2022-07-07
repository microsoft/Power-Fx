// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Interpreter.UDF;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx.Interpreter
{
    internal class UserDefinedTexlFunction : CustomTexlFunction, IAsyncTexlFunction
    {
        private readonly IEnumerable<NamedFormulaType> _parameterNames;
        private IExpression _expr;
        private readonly CheckWrapper _check;

        public override bool SupportsParamCoercion => false;

        public UserDefinedTexlFunction(string name, FormulaType returnType, IEnumerable<NamedFormulaType> parameterNames, CheckWrapper lazyCheck)
            : base(name, returnType, parameterNames.Select(x => x.Type).ToArray())
        {
            _parameterNames = parameterNames;
            _check = lazyCheck;
        }

        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancel)
        {
            // $$$ There's a lot of unnecessary string packing overhead here 
            // because Eval wants a Record rather than a resolved arg array.                 
            var parameters = FormulaValue.NewRecordFromFields(UDFHelper.Zip(_parameterNames.ToArray(), args));

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
