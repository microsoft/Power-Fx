using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;

namespace Microsoft.PowerFx.Functions
{
    internal static partial class Library
    {
        private enum ReturnBehavior
        {
            AlwaysEvaluateAndReturnResult,
            ReturnBlankIfAnyArgIsBlank,
            ReturnEmptyStringIfAnyArgIsBlank,
            ReturnFalseIfAnyArgIsBlank
        }

        private static bool IsInvalidDouble(double number)
        {
            return double.IsNaN(number) || double.IsInfinity(number);
        }

        /// <summary>
        /// A pipeline that maps blanks to a value, checks
        /// runtime types, and possibly map values to errors
        /// before filtering errors and possibly returning
        /// an ErrorValue instead of executing.
        /// </summary>
        /// <typeparam name="T">The specific FormulaValue type that the implementation of the builtin expects, for exmaple NumberValue for math functions.</typeparam>
        /// <param name="expandArguments">This stage of the pipeline can be used to expand an argument list if some of the arguments are optional and missing.</param>
        /// <param name="replaceBlankValues">This stage can be used to transform Blank() into something else, for example the number 0.</param>
        /// <param name="checkRuntimeTypes">This stage can be used to check to that all the arguments have type T, or check that all arguments have type T | Blank(), etc.</param>
        /// <param name="checkRuntimeValues">This stage can be used to generate errors if specific values occur in the arguments, for example infinity, NaN, etc.</param>
        /// <param name="returnBehavior">A flag that can be used to activate pre-defined early return behavior, such as returning Blank() if any argument is Blank().</param>
        /// <param name="targetFunction">The implementation of the builtin function.</param>
        /// <returns></returns>
        private static FunctionPtr StandardErrorHandling<T>(
                Func<IRContext, IEnumerable<FormulaValue>, IEnumerable<FormulaValue>> expandArguments,
                Func<IRContext, int, FormulaValue> replaceBlankValues,
                Func<IRContext, int, FormulaValue, FormulaValue> checkRuntimeTypes,
                Func<IRContext, int, FormulaValue, FormulaValue> checkRuntimeValues,
                ReturnBehavior returnBehavior,
                Func<EvalVisitor, SymbolContext, IRContext, T[], FormulaValue> targetFunction)
            where T : FormulaValue
        {
            return (runner, symbolContext, irContext, args) =>
            {
                var argumentsExpanded = expandArguments(irContext, args);

                var blankValuesReplaced = argumentsExpanded.Select((arg, i) =>
                {
                    if (arg is BlankValue)
                    {
                        return replaceBlankValues(arg.IRContext, i);
                    }
                    else
                    {
                        return arg;
                    }
                });

                var runtimeTypesChecked = blankValuesReplaced.Select((arg, i) => checkRuntimeTypes(irContext, i, arg));

                var runtimeValuesChecked = runtimeTypesChecked.Select((arg, i) =>
                {
                    if (arg is T t)
                    {
                        return checkRuntimeValues(arg.IRContext, i, t);
                    }
                    else
                    {
                        return arg;
                    }
                });

                var errors = runtimeValuesChecked.OfType<ErrorValue>();
                if (errors.Count() != 0)
                {
                    return ErrorValue.Combine(irContext, errors);
                }

                switch (returnBehavior)
                {
                    case ReturnBehavior.ReturnBlankIfAnyArgIsBlank:
                        if (runtimeValuesChecked.Any(arg => arg is BlankValue))
                        {
                            return new BlankValue(IRContext.NotInSource(FormulaType.Blank));
                        }

                        break;
                    case ReturnBehavior.ReturnEmptyStringIfAnyArgIsBlank:
                        if (runtimeValuesChecked.Any(arg => arg is BlankValue))
                        {
                            return new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty);
                        }

                        break;
                    case ReturnBehavior.ReturnFalseIfAnyArgIsBlank:
                        if (runtimeValuesChecked.Any(arg => arg is BlankValue))
                        {
                            return new BooleanValue(IRContext.NotInSource(FormulaType.Boolean), false);
                        }

                        break;
                    case ReturnBehavior.AlwaysEvaluateAndReturnResult:
                        break;
                }

                return targetFunction(runner, symbolContext, irContext, runtimeValuesChecked.Select(arg => arg as T).ToArray());
            };
        }

        // A wrapper that allows standard error handling to apply to
        // functions which accept the simpler parameter list of
        // an array of arguments, ignoring context, runner etc.
        private static FunctionPtr StandardErrorHandling<T>(
            Func<IRContext, IEnumerable<FormulaValue>, IEnumerable<FormulaValue>> expandArguments,
            Func<IRContext, int, FormulaValue> replaceBlankValues,
            Func<IRContext, int, FormulaValue, FormulaValue> checkRuntimeTypes,
            Func<IRContext, int, FormulaValue, FormulaValue> checkRuntimeValues,
            ReturnBehavior returnBehavior,
            Func<IRContext, T[], FormulaValue> targetFunction)
            where T : FormulaValue
        {
            return StandardErrorHandling<T>(expandArguments, replaceBlankValues, checkRuntimeTypes, checkRuntimeValues, returnBehavior, (runner, symbolContext, irContext, args) =>
            {
                return targetFunction(irContext, args);
            });
        }

        #region Common Arg Expansion Pipeline Stages
        private static Func<IRContext, IEnumerable<FormulaValue>, IEnumerable<FormulaValue>> InsertDefaultValues(int outputArgsCount, FormulaValue fillWith)
        {
            return (irContext, args) =>
            {
                var res = new List<FormulaValue>(args);
                while (res.Count < outputArgsCount)
                {
                    res.Add(fillWith);
                }

                return res.ToArray();
            };
        }

        private static IEnumerable<FormulaValue> MidFunctionExpandArgs(IRContext irContext, IEnumerable<FormulaValue> args)
        {
            var res = new List<FormulaValue>(args);
            while (res.Count < 3)
            {
                // The third argument to Mid can only ever be used if the first argument is a string
                if (args.First() is StringValue stringValue)
                {
                    var count = new NumberValue(IRContext.NotInSource(FormulaType.Number), stringValue.Value.Length);
                    res.Add(count);
                }
            }

            return res.ToArray();
        }
        #endregion

        #region Common Blank Replacement Pipeline Stages
        private static FormulaValue ReplaceBlankWithZero(IRContext irContext, int index)
        {
            return new NumberValue(IRContext.NotInSource(FormulaType.Number), 0.0);
        }

        private static FormulaValue ReplaceBlankWithEmptyString(IRContext irContext, int index)
        {
            return new StringValue(IRContext.NotInSource(FormulaType.String), string.Empty);
        }

        private static Func<IRContext, int, FormulaValue> ReplaceBlankWith(params FormulaValue[] values)
        {
            return (irContext, index) =>
            {
                return values[index];
            };
        }

        private static Func<IRContext, int, FormulaValue> ReplaceBlankWithZeroForSpecificIndices(params int[] indices)
        {
            var indicesToReplace = new HashSet<int>(indices);
            return (irContext, index) =>
            {
                if (indicesToReplace.Contains(index))
                {
                    return new NumberValue(IRContext.NotInSource(FormulaType.Number), 0.0);
                }

                return new BlankValue(irContext);
            };
        }
        #endregion

        #region Common Type Checking Pipeline Stages
        private static FormulaValue ExactValueType<T>(IRContext irContext, int index, FormulaValue arg)
            where T : FormulaValue
        {
            if (arg is T || arg is ErrorValue)
            {
                return arg;
            }
            else
            {
                return CommonErrors.RuntimeTypeMismatch(irContext);
            }
        }

        private static FormulaValue ExactValueTypeOrBlank<T>(IRContext irContext, int index, FormulaValue arg)
            where T : FormulaValue
        {
            if (arg is BlankValue)
            {
                return arg;
            }
            else
            {
                return ExactValueType<T>(irContext, index, arg);
            }
        }

        private static Func<IRContext, int, FormulaValue, FormulaValue> ExactSequence(params Func<IRContext, int, FormulaValue, FormulaValue>[] runtimeChecks)
        {
            return (irContext, index, arg) =>
            {
                return runtimeChecks[index](irContext, index, arg);
            };
        }

        private static FormulaValue AddColumnsTypeChecker(IRContext irContext, int index, FormulaValue arg)
        {
            if (index == 0)
            {
                return ExactValueTypeOrBlank<TableValue>(irContext, index, arg);
            }
            else if (index % 2 == 1)
            {
                return ExactValueTypeOrBlank<StringValue>(irContext, index, arg);
            }
            else
            {
                return ExactValueTypeOrBlank<LambdaFormulaValue>(irContext, index, arg);
            }
        }

        private static FormulaValue DateOrDateTime(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is DateValue || arg is DateTimeValue || arg is BlankValue || arg is ErrorValue)
            {
                return arg;
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }

        private static FormulaValue TimeOrDateTime(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is TimeValue || arg is DateTimeValue || arg is BlankValue || arg is ErrorValue)
            {
                return arg;
            }

            return CommonErrors.RuntimeTypeMismatch(irContext);
        }
        #endregion

        #region Common Runtime Value Checking Pipeline Stages
        private static FormulaValue FiniteChecker(IRContext irContext, int index, FormulaValue arg)
        {
            if (arg is NumberValue numberValue)
            {
                var number = numberValue.Value;
                if (IsInvalidDouble(number))
                {
                    return CommonErrors.ArgumentOutOfRange(irContext);
                }
            }

            return arg;
        }

        private static FormulaValue PositiveNumberChecker(IRContext irContext, int index, FormulaValue arg)
        {
            var finiteCheckResult = FiniteChecker(irContext, index, arg);
            if (finiteCheckResult is NumberValue numberArg)
            {
                var number = numberArg.Value;
                if (number < 0)
                {
                    return CommonErrors.ArgumentOutOfRange(irContext);
                }
            }

            return arg;
        }

        private static FormulaValue StrictPositiveNumberChecker(IRContext irContext, int index, FormulaValue arg)
        {
            var finiteCheckResult = FiniteChecker(irContext, index, arg);
            if (finiteCheckResult is NumberValue numberArg)
            {
                var number = numberArg.Value;
                if (number <= 0)
                {
                    return CommonErrors.ArgumentOutOfRange(irContext);
                }
            }

            return arg;
        }

        private static FormulaValue DivideByZeroChecker(IRContext irContext, int index, FormulaValue arg)
        {
            var finiteCheckResult = FiniteChecker(irContext, index, arg);
            if (index == 1 && finiteCheckResult is NumberValue numberArg)
            {
                var number = numberArg.Value;
                if (number == 0)
                {
                    return CommonErrors.DivByZeroError(irContext);
                }
            }

            return arg;
        }

        private static FormulaValue ReplaceChecker(IRContext irContext, int index, FormulaValue arg)
        {
            if (index == 1)
            {
                if (arg is BlankValue)
                {
                    return new ErrorValue(irContext, new ExpressionError()
                    {
                        Message = "The second parameter to the Replace function cannot be Blank()",
                        Span = irContext.SourceContext,
                        Kind = ErrorKind.InvalidFunctionUsage
                    });
                }

                var finiteCheckResult = FiniteChecker(irContext, index, arg);
                if (finiteCheckResult is NumberValue numberArg)
                {
                    var number = numberArg.Value;
                    if (number <= 0)
                    {
                        return CommonErrors.ArgumentOutOfRange(irContext);
                    }
                }

                return finiteCheckResult;
            }

            if (index == 2)
            {
                return PositiveNumberChecker(irContext, index, arg);
            }

            return arg;
        }
        #endregion

        #region No Op Pipeline Stages
        private static IEnumerable<FormulaValue> NoArgExpansion(IRContext irContext, IEnumerable<FormulaValue> args)
        {
            return args;
        }

        private static FormulaValue DoNotReplaceBlank(IRContext irContext, int index)
        {
            return new BlankValue(irContext);
        }

        // This function should be used when type checking is too unique and needs to be located
        // within the body of the builtin function itself
        private static FormulaValue DeferRuntimeTypeChecking(IRContext irContext, int index, FormulaValue arg)
        {
            return arg;
        }

        private static FormulaValue DeferRuntimeValueChecking<T>(IRContext irContext, int index, T arg)
            where T : FormulaValue
        {
            return arg;
        }
        #endregion
    }
}
