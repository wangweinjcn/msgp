﻿namespace Ace.Web.Mvc.Middlewares
{
    public class BaseResponseResult
    {
        public string Code { get; set; }

        public string Message { get; set; }
    }

    public class RequestInfo
    {
        public string ApplicationId { get; set; }
        public string Timestamp { get; set; }
        public string Nonce { get; set; }
        public string Sinature { get; set; }
        public string ApplicationPassword { get; set; }
    }

    public class ApplicationInfo
    {
        public string ApplicationId { get; set; }

        public string ApplicationName { get; set; }

        public string ApplicationPassword { get; set; }
    }

    public class CommonResult<T> : BaseResponseResult where T : class
    {
        public T Data { get; set; }
    }

    
}
