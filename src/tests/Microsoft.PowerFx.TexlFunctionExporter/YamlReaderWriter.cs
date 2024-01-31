// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using YamlDotNet.Serialization;

namespace Microsoft.PowerFx.TexlFunctionExporter
{
    public class YamlReaderWriter
    {
        private static readonly ISerializer _serializer = new SerializerBuilder().ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitEmptyCollections | DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitNull).Build();
        private static readonly IDeserializer _deserializer = new DeserializerBuilder().Build();

        public string GetYaml()
        {
            return _serializer.Serialize(this);
        }

        public static T ReadYaml<T>(string yaml)
            where T : YamlReaderWriter
        {
            return _deserializer.Deserialize<T>(yaml);
        }
    }
}
