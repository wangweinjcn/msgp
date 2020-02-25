using System;
using System.Xml.Serialization;

namespace eurekaServer.Models
{
    [XmlRoot("instance")]
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}