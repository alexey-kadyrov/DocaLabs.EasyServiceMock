using System;
using DocaLabs.EasyServiceMock;

namespace DocaLabs.EasyServcieMock.WcfMock.Service1Mock
{
    public class Service1 : IService1
    {
        static readonly WcfEndpointMock GetDataMock = new WcfEndpointMock();

        public ServiceResponseData GetData(string id)
        {
            var data = GetDataMock.FromJson<ServiceResponseData>("application/json", id);

            if (data != null)
                data.LastRequested = DateTime.UtcNow;

            return data;
        }

        // if there is no need to post process the response model then it can be even simpler
        //public Stream GetData(string id)
        //{
        //    return GetDataMock.Reply("application/json", id);
        //}

        public void SetupErrorForGetData(string id, string status)
        {
            GetDataMock.SetupError(status, id);
        }

        public void SetupResponseForGetData(string id, ServiceResponseData responseData)
        {
            GetDataMock.SetupJsonResponse(responseData, id);
        }
    }
}
