using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace DocaLabs.EasyServiceMock
{
    public class WebApiEndpointMock : EndpointMockBase
    {
        public HttpResponseMessage Reply(string contentType, params string[] correlationValues)
        {
            var methodInfo = new StackFrame(1).GetMethod();
            return Reply(methodInfo.DeclaringType, methodInfo.Name, contentType, correlationValues);
        }

        public HttpResponseMessage Reply(Type serviceType, string method, string contentType, params string[] correlationValues)
        {
            var correlationKey = MakeCorrelationKey(correlationValues);
            if(string.IsNullOrWhiteSpace(correlationKey))
                return SetError("There is no correlation key.", HttpStatusCode.BadRequest);

            var status = TryGetError(correlationKey);
            if (status != HttpStatusCode.OK)
                return SetError(string.Format("Error for '{0}'", correlationKey), status);

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);

            var response = TryGetResponse(correlationKey);
            if (response != null)
            {
                responseMessage.Content = new ByteArrayContent(response);
            }
            else
            {
                var data = GetResourceStream(serviceType, method, correlationKey) 
                    ?? GetResourceStream(serviceType, method, "default");

                if(data == null)
                    return SetError(string.Format("Key: '{0}' not found", correlationKey), HttpStatusCode.NotFound);

                if(data.ProposedStatus != HttpStatusCode.OK)
                    return SetError(string.Format("Key: '{0}', Error: '{1}'", correlationKey, data.ProposedDescription), data.ProposedStatus);

                responseMessage.Content = new StreamContent(data.Stream);
            }

            responseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            return responseMessage;
        }

        static HttpResponseMessage SetError(string description, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        {
            description = description.Replace("\r", " ");
            description = description.Replace("\n", " ");

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(description)
            };
        }
   }
}