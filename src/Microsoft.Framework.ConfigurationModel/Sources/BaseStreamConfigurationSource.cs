using System;
using System.IO;

namespace Microsoft.Framework.ConfigurationModel
{
    public abstract class BaseStreamConfigurationSource : BaseConfigurationSource
    {
        protected readonly IConfigurationStreamHandler streamHandler;

        public BaseStreamConfigurationSource(IConfigurationStreamHandler streamHandler, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(Resources.Error_InvalidFilePath, "path");
            }

            Path = PathResolver.ResolveAppRelativePath(path);

            if (streamHandler == null)
                throw new ArgumentNullException("streamHandler", "A configuration stream handler must be provided.");

            this.streamHandler = streamHandler;
        }

        public string Path { get; private set; }

        public override void Load()
        {
            using (var stream = streamHandler.ReadStream(Path))
            {
                Load(stream);
            }
        }

        public virtual void Commit()
        {
            // If the config file is not found in given path
            // i.e. we don't have a template to follow when generating contents of new config file
            if (!streamHandler.DoesStreamExist(Path))
            {
                var newConfigFileStream = streamHandler.CreateStream(Path);

                try
                {
                    // Generate contents to the newly created config file
                    GenerateNewConfig(newConfigFileStream);

                    //  Write the newly created config
                    streamHandler.WriteStream(newConfigFileStream, Path);
                }
                catch
                {
                    newConfigFileStream.Dispose();

                    // The operation should be atomic because we don't want a corrupted config file
                    // So we roll back if the operation fails
                    if (streamHandler.DoesStreamExist(Path))
                    {
                        streamHandler.DeleteStream(Path);
                    }

                    // Rethrow the exception
                    throw;
                }
                finally
                {
                    newConfigFileStream.Dispose();
                }

                return;
            }

            // Because we need to read the original contents while generating new contents, the new contents are
            // cached in memory and used to overwrite original contents after we finish reading the original contents
            using (var cacheStream = new MemoryStream())
            {
                using (var inputStream = streamHandler.ReadStream(Path))
                {
                    Commit(inputStream, cacheStream);
                }

                // Use the cached new contents to overwrite original contents
                cacheStream.Seek(0, SeekOrigin.Begin);

                streamHandler.WriteStream(cacheStream, Path);
            }
        }

        internal abstract void GenerateNewConfig(Stream outputStream);

        internal abstract void Load(Stream stream);

        internal abstract void Commit(Stream inputStream, Stream outputStream);
    }
}