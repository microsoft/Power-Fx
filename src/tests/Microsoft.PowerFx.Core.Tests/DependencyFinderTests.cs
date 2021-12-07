using System;
using System.Linq;
using Microsoft.PowerFx.Core.Syntax;
using Microsoft.PowerFx.Core.Syntax.Visitors;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    public class DependencyFinderTests
    {
        [Theory]
        // simple cases
        [InlineData("", "")]
        [InlineData("5", "")]
        [InlineData("true", "")]
        [InlineData("\"hello\" & \"world\"", "")]
        [InlineData("A", "A")]
        [InlineData("Blank()", "")]
        [InlineData("5 B", "")] // error
        [InlineData("A As B", "")] // error
        // Parent, Self, replaceable nodes not currently supported
        [InlineData("Parent", "")]
        [InlineData("Parent.A", "")]
        [InlineData("Self", "")]
        [InlineData("Self.A", "")]
        [InlineData("%placeholder%", "")]
        [InlineData("%placeholder%.A", "")]
        // composite expressions
        [InlineData("A;B;C", "A,B,C")]
        [InlineData("[A,B,C]", "A,B,C")]
        [InlineData("A.B", "A.B")]
        [InlineData("A.B.C", "A.B.C")]
        [InlineData("If(A)", "")] // error
        [InlineData("If(A, B, C)", "A,B,C")]
        [InlineData("If(A, B, C, D, E).P", "A,B.P,C,D.P,E.P")]
        [InlineData("If(A, B, C).Name", "A,B.Name,C.Name")]
        [InlineData("[If(A, B, C).Name,D,E]", "A,B.Name,C.Name,D,E")]
        [InlineData("Switch(A)", "")] // Error
        [InlineData("Switch(A, B)", "")] // Error
        [InlineData("Switch(Slider1.Value, 20, Result1, 10, Result2, 0, Result3)", "Result1,Result2,Result3,Slider1.Value")]
        [InlineData("Switch(Slider1.Value, 20, Result1, 10, Result2, 0, Result3).Name", "Result1.Name,Result2.Name,Result3.Name,Slider1.Value")]
        [InlineData("Switch(Slider1.Value, 20, Result1, 10, Result2, 0, Result3, DefaultResult)", "Result1,Result2,Result3,DefaultResult,Slider1.Value")]
        [InlineData("Switch(Slider1.Value, 20, Result1, 10, Result2, 0, Result3, DefaultResult).Name", "Result1.Name,Result2.Name,Result3.Name,DefaultResult.Name,Slider1.Value")]
        [InlineData("With(A).Name", "")] // error
        [InlineData("With({ B: 1, C: B.Inner } As Y, Y.C)", "B.Inner")]
        [InlineData("With(A, B).Name", "A.B.Name,B.Name")]
        [InlineData("With(A As B, B).Name", "A.Name")]
        [InlineData("With(User, Age + 1)", "User.Age,Age")]
        [InlineData(@"With( { AnnualRate: RateSlider/8/100,         // slider moves in 1/8th increments and convert to decimal
                              Amount: AmountSlider*10000,           // slider moves by 10,000 increment
                              Years: YearsSlider,                   // slider moves in single year increments, no adjustment required
                              AnnualPayments: 12 },                 // number of payments per year
                              With( { r: AnnualRate/AnnualPayments, // interest rate
                                      P: Amount,                    // loan amount
                                      n: Years*AnnualPayments },    // number of payments
                                    r*P / (1 - (1+r)^-n)            // standard interest calculation
                              )
                          )", "RateSlider,AmountSlider,YearsSlider")]
        [InlineData(@"With( { AnnualRate: RateSlider/8/100,         // slider moves in 1/8th increments and convert to decimal
                              Amount: AmountSlider*10000,           // slider moves by 10,000 increment
                              Years: YearsSlider,                   // slider moves in single year increments, no adjustment required
                              AnnualPayments: 12 },                 // number of payments per year
                              With( { r: AnnualRate/AnnualPayments, // interest rate
                                      P: Amount,                    // loan amount
                                      n: Years*AnnualPayments,      // number of payments
                                      x: Years },    
                                    r*P / (1 - (1+r)^-n) + x.N
                              )
                          )", "RateSlider,AmountSlider,YearsSlider,YearsSlider.N")]
        [InlineData(@"With( Patch( Orders, Defaults( Orders ), { OrderStatus: ""New"" } ),
                          ForAll(NewOrderDetails,
                                  Patch(OrderDetails, Defaults(OrderDetails),
                                         { Order: OrderID,          // from With's first argument, primary key of Patch result
                                           Quantity: Quantity,      // from ForAll's NewOrderDetails table
                                           ProductID: ProductID }   // from ForAll's NewOrderDetails table
                                  )
                          )
                    )", "NewOrderDetails,OrderDetails,OrderID,Quantity,ProductID")]
        public void DependencyFinder_ResolvesState(string expression, string expectedStateCsv)
        {
            string[] expectedState = expectedStateCsv.Split(",", StringSplitOptions.RemoveEmptyEntries);

            Formula formula = new Formula(expression);
            formula.EnsureParsed(Parser.TexlParser.Flags.All);
            var paths = FindDependenciesVisitor.Run(formula.ParseTree);

            var dottedSyntaxPaths = paths.Select(dpath => dpath.ToDottedSyntax());

            Assert.Equal(expectedState.OrderBy(s => s), dottedSyntaxPaths.OrderBy(s => s));
        }
    }
}
