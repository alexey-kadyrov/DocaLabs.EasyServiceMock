using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace DocaLabs.EasyServiceMock
{
    public abstract class EndpointMockBase
    {
        readonly ConcurrentDictionary<string, byte[]> _responses = new ConcurrentDictionary<string, byte[]>();
        readonly ConcurrentDictionary<string, HttpStatusCode> _errors = new ConcurrentDictionary<string, HttpStatusCode>();

        public void SetupResponse(Stream data, params string[] correlationValues)
        {
            using(var tmpStream = new MemoryStream())
            {
                data.CopyTo(tmpStream);
                _responses[MakeCorrelationKey(correlationValues)] = tmpStream.ToArray();
            }
        }

        public void SetupResponse(string data, params string[] correlationValues)
        {
            _responses[MakeCorrelationKey(correlationValues)] = data != null
                ? Encoding.UTF8.GetBytes(data)
                : null;
        }

        public void SetupJsonResponse(object data, params string[] correlationValues)
        {
            SetupResponse(JsonConvert.SerializeObject(data), correlationValues);
        }

        public void SetupError(string status, params string[] correlationValues)
        {
            _errors[MakeCorrelationKey(correlationValues)] = (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), status, true);
        }

        public void SetupError(HttpStatusCode status, params string[] correlationValues)
        {
            _errors[MakeCorrelationKey(correlationValues)] = status;
        }

        public string GetFormValue(Stream formData, string name)
        {
            return GetFormValue(StreamToString(formData), name);
        }

        public string[] GetFormValues(Stream formData, params string[] names)
        {
            return GetFormValues(StreamToString(formData), names);
        }

        public string GetFormValue(string formData, string name)
        {
            string value = null;
            if (!string.IsNullOrWhiteSpace(formData))
                value = HttpUtility.ParseQueryString(formData)[name];

            return !string.IsNullOrWhiteSpace(value) ? value : null;
        }

        public string[] GetFormValues(string formData, params string[] names)
        {
            if (string.IsNullOrWhiteSpace(formData) || names == null || names.Length == 0)
                return new string[0];

            var values = HttpUtility.ParseQueryString(formData);

            return names.Select(name => values[name]).ToArray();
        }

        public T FromJson<T>(Stream stream) where T: class
        {
            if (stream == null)
                return null;

            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                return new JsonSerializer().Deserialize<T>(reader);
            }
        }

        protected virtual string MakeCorrelationKey(params string[] correlationValues)
        {
            if (correlationValues == null || correlationValues.Length == 0)
                return null;

            return string.Join(".", correlationValues);
        }

        protected byte[] TryGetResponse(string correlationKey)
        {
            byte[] response;
            return _responses.TryGetValue(correlationKey, out response)
                ? response
                : null;
        }

        protected HttpStatusCode TryGetError(string correlationKey)
        {
            HttpStatusCode status;
            return _errors.TryGetValue(correlationKey, out status)
                ? status
                : HttpStatusCode.OK;
        }

        static protected ResourceStream GetResourceStream(Type serviceType, string method, string key)
        {
            try
            {
                var assembly = serviceType.Assembly;

                var resourceKey = string.Format("{0}.{1}.{2}", serviceType.Namespace, method, key);
                var resourceAsStream = assembly.GetManifestResourceStream(resourceKey) ??
                                       assembly.GetManifestResourceStream(resourceKey.Replace("." + key, "." + key.ToLower()));

                if (resourceAsStream != null) 
                    return new ResourceStream(resourceAsStream);

                if (key == "default")
                    throw new FileNotFoundException(string.Format("Problem opening stub file [{0}].", key));

                return null;
            }
            catch (Exception e)
            {
                if (key == "default")
                    throw new Exception(string.Format("Problem opening stub file [{0}]. See inner exception for more detail", key), e);

                return null;
            }
        }

        static string StreamToString(Stream data)
        {
            using (var reader = new StreamReader(data))
            {
                return reader.ReadToEnd();
            }
        }

        protected class ResourceStream
        {
            public Stream Stream { get; private set; }
            public HttpStatusCode ProposedStatus { get; private set; }
            public string ProposedDescription { get; private set; }

            public ResourceStream(Stream stream)
            {
                Stream = stream;

                InitializeProposedStatus();
            }

            void InitializeProposedStatus()
            {
                // ReSharper disable EmptyGeneralCatchClause
                if (Stream == null)
                {
                    ProposedStatus = HttpStatusCode.NotFound;
                    return;
                }

                ProposedStatus = HttpStatusCode.OK;

                try
                {
                    // intentionally not disposing
                    var reader = new StreamReader(Stream);

                    var content = reader.ReadLine();
                    if (content == null || !content.StartsWith("return error:", StringComparison.OrdinalIgnoreCase))
                        return;

                    ProposedStatus = (HttpStatusCode)int.Parse(content.Substring(13));
                    ProposedDescription = reader.ReadToEnd();
                }
                catch
                {
                }
                finally
                {
                    Stream.Seek(0, SeekOrigin.Begin);
                }
                // ReSharper restore EmptyGeneralCatchClause
            }
        }
   }
}