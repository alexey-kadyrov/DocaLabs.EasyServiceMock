using System.ServiceModel;
using System.ServiceModel.Web;

namespace DocaLabs.EasyServcieMock.WcfMock.Service1Mock
{
    [ServiceContract]
    public interface IService1
    {
        [OperationContract, WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/GetData/{id}")]
        ServiceResponseData GetData(string id);

        [OperationContract, WebInvoke(Method = "POST", UriTemplate = "/SetupErrorForGetData/{id}?error={status}")]
        void SetupErrorForGetData(string id, string status);

        [OperationContract, WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Json, UriTemplate = "/SetupResponseForGetData/{id}")]
        void SetupResponseForGetData(string id, ServiceResponseData responseData);
    }
}
