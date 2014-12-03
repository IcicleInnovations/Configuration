using System;
using System.IO;

namespace Microsoft.Framework.ConfigurationModel
{
    public interface IConfigurationStreamHandler
	{
		Stream CreateStream(string path);

		void DeleteStream(string path);

		bool DoesStreamExist(string path);

		Stream ReadStream(string path);

		void WriteStream(Stream stream, string path);
	}
}