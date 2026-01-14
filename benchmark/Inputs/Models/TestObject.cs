using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace PowerFXBenchmark.Inputs.Models
{
    public class TestObject
    {
        public string Id { get; set; }

        public string TestSessionId { get; set; }

        public RootMetadata RootMetadata { get; set; } = new RootMetadata();

        public IDictionary<string, JToken> JTokenBag { get; set; } = new Dictionary<string, JToken>();

        public RootMetadata GetMetadata() => RootMetadata;

#pragma warning disable CS8601 // Possible null reference assignment.
        public bool TryGetProperty(string name, out JToken property) => JTokenBag.TryGetValue(name, out property);
#pragma warning restore CS8601 // Possible null reference assignment.
    }

    public class RootMetadata : PropertyMetadata
    {
        public IDictionary<string, PropertyMetadata> PropertyMetadata { get; set; } = new Dictionary<string, PropertyMetadata>();
    }


    public class PropertyMetadata
    {
        public string Type { get; set; }

        public string Time { get; set; }
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
