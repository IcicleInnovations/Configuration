// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

using Resources = Microsoft.Framework.ConfigurationModel.Json.Resources;

namespace Microsoft.Framework.ConfigurationModel
{
    public class JsonConfigurationSource : BaseStreamConfigurationSource, ICommitableConfigurationSource
    {
        public JsonConfigurationSource(string path)
            : this(new FileConfigurationStreamHandler(), path)
        { }

        public JsonConfigurationSource(IConfigurationStreamHandler streamHandler, string path)
            : base(streamHandler, path)
        { }

        internal override void Load(Stream stream)
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                var startObjectCount = 0;

                // Dates are parsed as strings
                reader.DateParseHandling = DateParseHandling.None;

                // Move to the first token
                reader.Read();

                SkipComments(reader);

                if (reader.TokenType != JsonToken.StartObject)
                {
                    throw new FormatException(Resources.FormatError_RootMustBeAnObject(reader.Path,
                        reader.LineNumber, reader.LinePosition));
                }

                do
                {
                    SkipComments(reader);

                    switch (reader.TokenType)
                    {
                        case JsonToken.StartObject:
                            startObjectCount++;
                            break;

                        case JsonToken.EndObject:
                            startObjectCount--;
                            break;

                        // Keys in key-value pairs
                        case JsonToken.PropertyName:
                            break;

                        // Values in key-value pairs
                        case JsonToken.Integer:
                        case JsonToken.Float:
                        case JsonToken.String:
                        case JsonToken.Boolean:
                        case JsonToken.Bytes:
                        case JsonToken.Raw:
                        case JsonToken.Null:
                            var key = reader.Path.Replace(".", Constants.KeyDelimiter);
                            if (data.ContainsKey(key))
                            {
                                throw new FormatException(Resources.FormatError_KeyIsDuplicated(key));
                            }
                            data[key] = reader.Value.ToString();
                            break;

                        // End of file
                        case JsonToken.None:
                            {
                                throw new FormatException(Resources.FormatError_UnexpectedEnd(reader.Path,
                                    reader.LineNumber, reader.LinePosition));
                            }

                        default:
                            {
                                // Unsupported elements: Array, Constructor, Undefined
                                throw new FormatException(Resources.FormatError_UnsupportedJSONToken(
                                    reader.TokenType, reader.Path, reader.LineNumber, reader.LinePosition));
                            }
                    }

                    reader.Read();

                } while (startObjectCount > 0);
            }

            ReplaceData(data);
        }

        // Use the original file as a template while generating new file contents
        // to make sure the format is consistent and comments are not lost
        internal override void Commit(Stream inputStream, Stream outputStream)
        {
            var processedKeys = new HashSet<string>();
            var outputWriter = new JsonTextWriter(new StreamWriter(outputStream));
            outputWriter.Formatting = Formatting.Indented;

            using (var inputReader = new JsonTextReader(new StreamReader(inputStream)))
            {
                var startObjectCount = 0;

                // Dates are parsed as strings
                inputReader.DateParseHandling = DateParseHandling.None;

                // Move to the first token
                inputReader.Read();

                CopyComments(inputReader, outputWriter);

                if (inputReader.TokenType != JsonToken.StartObject)
                {
                    throw new FormatException(Resources.FormatError_RootMustBeAnObject(inputReader.Path,
                        inputReader.LineNumber, inputReader.LinePosition));
                }

                do
                {
                    CopyComments(inputReader, outputWriter);

                    switch (inputReader.TokenType)
                    {
                        case JsonToken.StartObject:
                            outputWriter.WriteStartObject();
                            startObjectCount++;
                            break;

                        case JsonToken.EndObject:
                            outputWriter.WriteEndObject();
                            startObjectCount--;
                            break;

                        // Keys in key-value pairs
                        case JsonToken.PropertyName:
                            outputWriter.WritePropertyName(inputReader.Value.ToString());
                            break;

                        // Values in key-value pairs
                        case JsonToken.Integer:
                        case JsonToken.Float:
                        case JsonToken.String:
                        case JsonToken.Boolean:
                        case JsonToken.Bytes:
                        case JsonToken.Raw:
                        case JsonToken.Null:
                            var key = inputReader.Path.Replace(".", Constants.KeyDelimiter);
                            if (!Data.ContainsKey(key))
                            {
                                throw new InvalidOperationException(Resources.FormatError_CommitWhenNewKeyFound(key));
                            }
                            outputWriter.WriteValue(Data[key]);
                            processedKeys.Add(key);
                            break;

                        // End of file
                        case JsonToken.None:
                            {
                                throw new FormatException(Resources.FormatError_UnexpectedEnd(inputReader.Path,
                                    inputReader.LineNumber, inputReader.LinePosition));
                            }

                        default:
                            {
                                // Unsupported elements: Array, Constructor, Undefined
                                throw new FormatException(Resources.FormatError_UnsupportedJSONToken(
                                    inputReader.TokenType, inputReader.Path, inputReader.LineNumber,
                                    inputReader.LinePosition));
                            }
                    }

                    inputReader.Read();

                } while (startObjectCount > 0);

                CopyComments(inputReader, outputWriter);
                outputWriter.Flush();
            }

            if (Data.Count() != processedKeys.Count())
            {
                var missingKeys = string.Join(", ", Data.Keys.Except(processedKeys));
                throw new InvalidOperationException(Resources.FormatError_CommitWhenKeyMissing(missingKeys));
            }
        }

        // Write the contents of newly created config file to given stream
        internal override void GenerateNewConfig(Stream outputStream)
        {
            var outputWriter = new JsonTextWriter(new StreamWriter(outputStream));
            outputWriter.Formatting = Formatting.Indented;

            outputWriter.WriteStartObject();
            foreach (var entry in Data)
            {
                outputWriter.WritePropertyName(entry.Key);
                outputWriter.WriteValue(entry.Value);
            }
            outputWriter.WriteEndObject();
        }

        private void SkipComments(JsonReader reader)
        {
            while (reader.TokenType == JsonToken.Comment)
            {
                reader.Read();
            }
        }

        private void CopyComments(JsonReader inputReader, JsonWriter outputStream)
        {
            while (inputReader.TokenType == JsonToken.Comment)
            {
                outputStream.WriteComment(inputReader.Value.ToString());
                inputReader.Read();
            }
        }
    }
}
