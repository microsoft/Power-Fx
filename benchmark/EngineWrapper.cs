using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json.Linq;
using PowerFXBenchmark.Builders;
using PowerFXBenchmark.Inputs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerFXBenchmark
{
    public class EngineWrapper
    {
        public readonly RecalcEngine engine;

        public EngineWrapper()
        {
            engine = new RecalcEngine(new PowerFxConfig());
        }

        public IExpression CompiledSingleExpression(string expression, RecordType inputType)
        {
            var parsedResult = engine.Parse(expression, new ParserOptions { AllowsSideEffects = false });
            var checkedResult = engine.Check(parsedResult, inputType);
            checkedResult.ThrowOnErrors();

            return checkedResult.Expression;
        }

        public Task<object> EvaluateExpressionAsync(IExpression expression, TestObject twin, string telemetry, CancellationToken cancellationToken = default)
        {
            var builder = new RecordValueBuilder();
            var parameters = builder
                .WithTestObject(twin)
                .WithEventJson(telemetry)
                .Build();

            return EvaluateExpressionAsync(expression, parameters, cancellationToken);
        }

        public async Task<object> EvaluateExpressionAsync(IExpression expression, RecordValue parameters, CancellationToken cancellationToken = default)
        {
            var result = await expression.EvalAsync(parameters, cancellationToken).ConfigureAwait(false);
            return result.ToObject();
        }
    }
}
