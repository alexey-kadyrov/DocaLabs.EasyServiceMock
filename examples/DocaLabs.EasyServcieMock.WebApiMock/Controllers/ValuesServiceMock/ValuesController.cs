using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DocaLabs.EasyServiceMock;

namespace DocaLabs.EasyServcieMock.WebApiMock.Controllers.ValuesServiceMock
{
    public class ValuesController : ApiController
    {
        static readonly WebApiEndpointMock GetMock = new WebApiEndpointMock();

        // GET api/values
        public HttpResponseMessage Get()
        {
            return GetMock.Reply("application/json", "list");
        }

        // GET api/values/5
        public HttpResponseMessage Get(int id)
        {
            return GetMock.Reply("application/json", id.ToString(CultureInfo.InvariantCulture));
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
            GetMock.Store(value, id.ToString(CultureInfo.InvariantCulture));
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
            GetMock.SetupError(HttpStatusCode.NotFound, id.ToString(CultureInfo.InvariantCulture));
        }
    }
}
