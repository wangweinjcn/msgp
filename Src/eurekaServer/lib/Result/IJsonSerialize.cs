using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
namespace Ace
{
    public interface IJsonSerialize
    {
        string toJsonString();
    }
}
