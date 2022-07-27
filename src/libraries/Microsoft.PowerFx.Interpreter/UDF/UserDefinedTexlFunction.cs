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
    internal class UserDefinedTexlFunction : CustomTexlFunction
    {
        private readonly IEnumerable<NamedFormulaType> _parameterNames;
        private ParsedExpression _expr;
        private readonly CheckWrapper _check;

        public override bool SupportsParamCoercion => false;

        public UserDefinedTexlFunction(string name, FormulaType returnType, IEnumerable<NamedFormulaType> parameterNames, CheckWrapper lazyCheck)
            : base(name, returnType, parameterNames.Select(x => x.Type).ToArray())
        {
            _parameterNames = parameterNames;
            _check = lazyCheck;
        }

        public async Task<FormulaValue> InvokeAsync(FormulaValue[] args, CancellationToken cancel, StackDepthCounter stackMarker)
        {
            // $$$ There's a lot of unnecessary string packing overhead here 
            // because Eval wants a Record rather than a resolved arg array.                 
            var parameters = FormulaValue.NewRecordFromFields(UDFHelper.Zip(_parameterNames.ToArray(), args));

            var result = await GetExpression().EvalAsyncInternal(parameters, cancel, stackMarker);

            return result;
        }

        public IEnumerable<ExpressionError> Bind()
        {
            var check = _check.Get();
            if (!check.IsSuccess)
            {
                return check.Errors;
            }

            if (check.Expression is ParsedExpression parsed)
            {
                _expr = parsed;
            } 
            else
            {
                var errorList = new List<ExpressionError>
                {
                    new ExpressionError()
                    {
                        Message = "Expression is not a ParsedExpression"
                    }
                };
                return errorList;
            }

            if (!check.ReturnType._type.Kind.Equals(ReturnType.Kind))
            {
                var errorList = new List<ExpressionError>
                {
                    new ExpressionError()
                    {
                        Message = "Return type does not match body return type"
                    }
                };
                return errorList;
            }

            return new List<ExpressionError>();
        }

        public ParsedExpression GetExpression()
        {
            if (_expr == null)
            {
                throw new UDFBindingMissingException();
            }

            return _expr;
        }
    }
}
