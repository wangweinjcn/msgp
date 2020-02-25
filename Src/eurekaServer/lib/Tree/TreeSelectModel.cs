using System;
using System.Collections.Generic;
using System.Linq;

namespace Ace.Web.Mvc.Common.Tree
{
    public class TreeSelectModel
    {
        public string id { get; set; }
        public string text { get; set; }
        public string parentId { get; set; }
        public object data { get; set; }
    }
}