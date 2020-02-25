using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FrmLib.Interface
{
  public  interface IAuthorizefilter
    {

        bool IsAllowed(string roleidlist, string controller, string action,
            string httpmethod,string paramJson=null,string areacode="-1",bool isdevice=false);
        bool AnonymousAllowed(string controllername, string actionname,
            string httpmethod,string paramJson=null,string areacode= "-1", bool isdevice = false);
        bool Isvalidtoken(string str_token);
        bool Isvalidtoken(string userId,string str_token );
        bool defaultallow {get;set;}
        bool IsHostAllowed(string remoteHost, string protocal);
    }
}
