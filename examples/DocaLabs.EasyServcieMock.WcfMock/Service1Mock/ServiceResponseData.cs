using System;
using System.Runtime.Serialization;

namespace DocaLabs.EasyServcieMock.WcfMock.Service1Mock
{
    [DataContract]
    public class ServiceResponseData
    {
        [DataMember]
        public string Value { get; set; }

        [DataMember]
        public DateTime LastRequested { get; set; }
    }
}