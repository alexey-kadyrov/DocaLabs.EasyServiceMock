using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.ServiceModel.Web;

namespace DocaLabs.EasyServiceMock
{
    public class WcfEndpointMock : EndpointMockBase
    {
        public T FromJson<T>(string contentType, params string[] correlationValues) where T : class
        {
            var methodInfo = new StackFrame(1).GetMethod();
            return FromJson<T>(Reply(methodInfo.DeclaringType, methodInfo.Name, contentType, correlationValues));
        }

        public Stream Reply(string contentType, params string[] correlationValues)
        {
            var methodInfo = new StackFrame(1).GetMethod();

            return Reply(methodInfo.DeclaringType, methodInfo.Name, contentType, correlationValues);
        }

        public Stream Reply(Type serviceType, string method, string contentType, params string[] correlationValues)
        {
            var correlationKey = MakeCorrelationKey(correlationValues);
            if (string.IsNullOrWhiteSpace(correlationKey))
            {
                SetError("There is no correlation key.", HttpStatusCode.BadRequest);
                return null;
            }

            RecordCall(correlationKey, method);

            var status = TryGetError(correlationKey);
            if (status != HttpStatusCode.OK)
            {
                SetError(string.Format("Error for '{0}'", correlationKey), status);
                return null;
            }

            if (WebOperationContext.Current == null) 
                throw new InvalidOperationException();
                
            WebOperationContext.Current.OutgoingResponse.ContentType = contentType;

            var response = TryGetResponse(correlationKey);
            if(response != null)
                return new MemoryStream(response);

            var data = GetResourceStream(serviceType, method, correlationKey) 
                ?? GetResourceStream(serviceType, method, "default");

            if (data == null)
            {
                SetError(string.Format("Key: '{0}' not found", correlationKey), HttpStatusCode.NotFound);
                return null;
            }

            if (data.ProposedStatus == HttpStatusCode.OK) 
                return data.Stream;

            SetError(string.Format("Key: '{0}', Error: '{1}'", correlationKey, data.ProposedDescription), data.ProposedStatus);
            
            return null;
        }

        public Stream StoreAndReply(Stream data, string contentType, params string[] correlationValues)
        {
            var methodInfo = new StackFrame(1).GetMethod();

            SetupResponse(data, correlationValues);

            return Reply(methodInfo.DeclaringType, methodInfo.Name, contentType, correlationValues);
        }

        public Stream StoreAndReply(string data, string contentType, params string[] correlationValues)
        {
            var methodInfo = new StackFrame(1).GetMethod();

            SetupResponse(data, correlationValues);

            return Reply(methodInfo.DeclaringType, methodInfo.Name, contentType, correlationValues);
        }

        public Stream StoreJsonAndReply(object data, string contentType, params string[] correlationValues)
        {
            var methodInfo = new StackFrame(1).GetMethod();

            SetupJsonResponse(data, correlationValues);

            return Reply(methodInfo.DeclaringType, methodInfo.Name, contentType, correlationValues);
        }

        static void SetError(string description, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        {
            if (WebOperationContext.Current == null)
                throw new Exception();

            var context = WebOperationContext.Current.OutgoingResponse;

            description = description.Replace("\r", " ");
            description = description.Replace("\n", " ");

            context.StatusCode = statusCode;
            context.StatusDescription = description;
        }
   }
}