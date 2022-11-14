// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.PowerFx.Types;
using Xunit;

namespace Microsoft.PowerFx.Minit
{
    public class TranslatorTests
    {
        [Theory]
        [InlineData("Sum(ProcessEvents, Duration)", "Sum(ProcessEvents, Duration())", 480.0)]
        public void Test1(string expressionFx, string expectedMinit, object scalarResult)
        {
            var converter = new Converter();

            // run on power Fx
            var result = Eval(expressionFx);
            Assert.Equal(scalarResult, result.ToObject());

            var actual = converter.Convert(expressionFx);

            Assert.Equal(expectedMinit, actual);
        }

        // Evaluate the fx expression against sample data with an interpreter. 
        private FormulaValue Eval(string expressionFx)
        {
            var cache = new TypeMarshallerCache();
            var fxEvents = cache.Marshal(_sampleEvents);
            var engine = new RecalcEngine();

            // Set builtin identifiers for the list. 
            engine.UpdateVariable(Converter.AllEvents, fxEvents);
            var result = engine.Eval(expressionFx);

            return result;
        }

        private enum User
        {
            Peter,
            Michal,
            Denis
        }

        private readonly MyEvent[] _sampleEvents = new MyEvent[]
        {
            new MyEvent(1, 1, "A", User.Peter, 10),
            new MyEvent(1, 1, "B", User.Michal, 20),
            new MyEvent(1, 1, "C", User.Michal, 60),
            new MyEvent(1, 2, "A", User.Peter, 40),
            new MyEvent(1, 2, "B", User.Denis, 20),
            new MyEvent(1, 2, "C", User.Denis, 60),
            new MyEvent(1, 2, "C", User.Michal, 60),
            new MyEvent(2, 3, "A", User.Denis, 10),
            new MyEvent(2, 3, "B", User.Peter, 20),
            new MyEvent(2, 3, "C", User.Michal, 180)
        };           

        private class MyEvent
        {
            public MyEvent() 
            { 
            }

            public MyEvent(int @case, int view, string activity, User user, int duration)
            {
                Case = @case;
                View = view;
                Activity = activity;
                User = user.ToString();
                Duration = duration;
            }

            public string User { get; set; }

            public string Activity { get; set; }

            public int Case { get; set; }
            
            public int View { get; set; }
            
            public int Duration { get; set; }
        }
    }
}
