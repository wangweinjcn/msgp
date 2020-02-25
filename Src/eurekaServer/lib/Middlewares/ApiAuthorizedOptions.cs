namespace Ace.Web.Mvc.Middlewares
{
    using FrmLib.Interface;
    public class ApiAuthorizedOptions
    {
        public string Name { get; set; }

        public string EncryptKey { get; set; }
        
        public int ExpiredSecond { get; set; }
        public bool isWeb { get; set; }
        public bool noAuthoReturnok { get; set; }
        public string loginUrl { get; set; }
        public string errorUrl { get; set; }
        
        public bool isUseCache { get; set; }
        public bool enableSwagger { get; set; }
        public string swaggerUrl { get; set; }
        public IAuthorizefilter authorizefilter { get; set; }

        public ApiAuthorizedOptions()
        {
            noAuthoReturnok = false;
            enableSwagger = false;
            swaggerUrl = "/Swagger";
        }
    }
}
