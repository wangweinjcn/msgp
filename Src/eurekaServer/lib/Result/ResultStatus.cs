using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ace
{
    public enum ResultStatus
    {
        OK = 100,
        Failed = 101,
        NotLogin = 102,
        Unauthorized = 103,
        SysError=500,
        Other1=801,
        Other2=802,
        Other3=803
    }
}
