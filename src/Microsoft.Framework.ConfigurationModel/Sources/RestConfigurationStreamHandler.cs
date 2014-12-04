using System;
using System.IO;
using System.Net.Http;
using System.Text;

namespace Microsoft.Framework.ConfigurationModel
{
    public class RestConfigurationStreamHandler : IConfigurationStreamHandler
    {
        public RestConfigurationStreamHandler(string apiRoot, string contentType)
        {
            if (string.IsNullOrEmpty(apiRoot))
                throw new ArgumentNullException("apiRoot", "An API Root must be provided.");

            ApiRoot = apiRoot.TrimEnd('/');

            ContentType = string.IsNullOrEmpty(contentType) ? "application/json" : contentType;
        }

        public string ApiRoot { get; private set; }

        public string ContentType { get; private set; }

        public virtual Stream CreateStream(string path)
        {
            var memStream = new MemoryStream();

            memStream.Write(Encoding.Default.GetBytes("{}"), 0, Int32.MaxValue);

            return memStream;
        }

        public virtual void DeleteStream(string path)
        {
            using (var client = new HttpClient())
            {
                var result = client.DeleteAsync(getFullPath(path)).Result;
            }
        }

        public virtual bool DoesStreamExist(string path)
        {
            using (var client = new HttpClient())
            {
                var result = client.GetAsync(getFullPath(path)).Result;

                return result.StatusCode == System.Net.HttpStatusCode.OK;
            }
        }

        public virtual Stream ReadStream(string path)
        {
            var fullPath = getFullPath(path);

            using (var client = new HttpClient())
            {
                var result = client.GetAsync(fullPath).Result;

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                    return result.Content.ReadAsStreamAsync().Result;
                else
                    throw new FileNotFoundException(string.Format("The configuration file could not be found at {0} with Http Status code {1}.", fullPath, result.StatusCode));
            }
        }

        public virtual void WriteStream(Stream stream, string path)
        {
            var fullPath = getFullPath(path);

            using (var client = new HttpClient())
            {
                var content = new StreamContent(stream);

                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(ContentType);

                var result = client.PostAsync(fullPath, content).Result;

                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                    throw new FileNotFoundException(string.Format("The configuration file could not be saved to {0} with Http Status code {1}.", fullPath, result.StatusCode));
            }
        }

        private string getFullPath(string relativePath)
        {
            return String.Format("{0}/{1}", ApiRoot, relativePath.TrimStart('/'));
        }
    }
}