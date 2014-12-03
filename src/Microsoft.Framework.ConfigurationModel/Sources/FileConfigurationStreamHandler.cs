using System;
using System.IO;

namespace Microsoft.Framework.ConfigurationModel
{
    public class FileConfigurationStreamHandler : IConfigurationStreamHandler
	{
		public virtual Stream CreateStream(string path)
		{
			return new FileStream(path, FileMode.CreateNew);
		}

		public virtual void DeleteStream(string path)
		{
			File.Delete(path);
        }

		public virtual bool DoesStreamExist(string path)
		{
			return File.Exists(path);
        }

		public virtual Stream ReadStream(string path)
		{
			return new FileStream(path, FileMode.Open, FileAccess.Read);
		}

		public virtual void WriteStream(Stream stream, string path)
		{
			using (var outputStream = new FileStream(path, FileMode.Truncate))
			{
				stream.CopyTo(outputStream);
			}
		}
	}
}