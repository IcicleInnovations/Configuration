// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.ConfigurationModel
{
    public static class JsonConfigurationExtension
    {
        public static IConfigurationSourceContainer AddJsonFile(this IConfigurationSourceContainer configuration, string path, IConfigurationStreamHandler streamHandler = null)
        {
			if (streamHandler == null)
				streamHandler = new FileConfigurationStreamHandler();

            configuration.Add(new JsonConfigurationSource(streamHandler, path));

            return configuration;
        }
    }
}
