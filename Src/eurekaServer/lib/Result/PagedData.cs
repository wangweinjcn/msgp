using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Ace
{
    public class PagedJObjData : IJsonSerialize
    {
        public PagedJObjData()
            : this(new JArray())
        {
        }
        public PagedJObjData(JArray dataList)
            : this(dataList, 0)
        {
        }
        public PagedJObjData(JArray dataList, int totalCount)
            : this(dataList, totalCount, 0, 0)
        {
        }
        public PagedJObjData(JArray dataList, int totalCount, int currentPage, int pageSize)
        : this(dataList, totalCount, currentPage, pageSize, "")
        {
        }
        #region vuetable2
        public PagedJObjData(JArray dataList, int totalCount, int currentPage, int pageSize, string basicUrl)
        {
            this.DataList = dataList;
            this.TotalCount = totalCount;
            this.CurrentPage = currentPage;
            this.PageSize = pageSize;
            this._basicUrl =  basicUrl ;
        }
        public virtual string toJsonString()
        {


            {
                JObject jobj = new JObject();
                jobj.Add("TotalCount", this.TotalCount);
                jobj.Add("PageSize", this.PageSize);
                jobj.Add("CurrentPage", this.CurrentPage);
                jobj.Add("TotalPage", this.TotalPage);
                if (string.IsNullOrEmpty(_basicUrl))
                    jobj.Add("DataList", DataList);
                else
                    jobj.Add("data", DataList);

                jobj.Add("prev_page_url", this.prev_page_url);
                jobj.Add("per_page", this.per_page);
                jobj.Add("next_page_url", this.next_page_url);
                jobj.Add("total", this.total);
                jobj.Add("current_page", this.current_page);
                jobj.Add("from", this.from);
                jobj.Add("to", this.to);
                jobj.Add("last_page", this.last_page);
                return jobj.ToString();

            }
        }
        #endregion vuetable2


        public PagedJObjData(Pagination paging)
            : this(paging.Page, paging.PageSize)
        {
        }
        public PagedJObjData(int currentPage, int pageSize)
            : this(new JArray(), 0, currentPage, pageSize)
        {
        }
        public PagedJObjData(int totalCount, int currentPage, int pageSize)
            : this(new JArray(), totalCount, currentPage, pageSize)
        {
        }

        public int TotalCount { get; set; }
        public int TotalPage
        {
            get
            {
                if (this.TotalCount > 0)
                {
                    return this.TotalCount % this.PageSize == 0 ? this.TotalCount / this.PageSize : this.TotalCount / this.PageSize + 1;
                }
                else
                {
                    return 0;
                }
            }
        }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        #region vuetable2
        private string _basicUrl;

        public int per_page {
            get {
                return PageSize;
            }
            set {
                PageSize = value;
            }
        }
        public int current_page {
            get { return CurrentPage; }
            set { CurrentPage = value; }
        }
        public int last_page {
            get { return TotalPage; }

        }
        public string next_page_url {
            get {
                var npage = (this.current_page >= TotalPage ? TotalPage.ToString() : (current_page + 1).ToString());
                return _basicUrl + (this._basicUrl.Contains("?") ? "&page=" + npage : "?page=" + npage);
                    
                    ;
            }
        }
        public string prev_page_url {
            get
            {
                var npage = (this.current_page <= 1 ? "0" : (current_page - 1).ToString());
                return _basicUrl + (this._basicUrl.Contains("?") ? "&page=" + npage : "?page=" + npage);

               
                    
                    ;
            }
        }
        public int from{
            get {
                return (current_page-1) * PageSize + 1;
            }
        }

        public int to {
            get {
                return (current_page ) * PageSize > TotalCount
                       ? TotalCount : (current_page ) * PageSize - 1;
            }
        }
        public JArray DataList { get; set; }
        /// <summary>
        /// vuetable-2 use;
        ///  total number of records available
        /// </summary>
        public int total { get
            {
                return TotalCount;
            }
            set {
                TotalCount = value;
            }
            }
        #endregion
    }
    public class PagedData : PagedData<object>
    {
        public PagedData()
            : base()
        {
        }
        public PagedData(IList<object> dataList)
            : base(dataList, 0)
        {
        }
        public PagedData(IList<object> dataList, int totalCount)
            : base(dataList, totalCount, 0, 0)
        {
        }
        public PagedData(IList<object> dataList, int totalCount, int currentPage, int pageSize)
            : base(dataList, totalCount, currentPage, pageSize)
        {
        }
        #region vuetable2
        public PagedData(IList<object> dataList, int totalCount, int currentPage, int pageSize, string basicUrl)
            :base(dataList, totalCount, currentPage, pageSize,basicUrl)
        {
                    }
        #endregion vuetable2
        public PagedData(Pagination paging)
            : base(paging)
        {
        }
        public PagedData(int currentPage, int pageSize)
            : base(currentPage, pageSize)
        {
        }
        public PagedData(int totalCount, int currentPage, int pageSize)
            : base(new List<object>(), totalCount, currentPage, pageSize, null)
        {
        }
    }

    public class PagedData<T> : IJsonSerialize
    {
        public PagedData()
            : this(new List<T>())
        {
        }
        public PagedData(IList<T> dataList)
            : this(dataList, 0)
        {
        }
        public PagedData(IList<T> dataList, int totalCount)
            : this(dataList, totalCount, 0, 0)
        {
        }
        public PagedData(IList<T> dataList, int totalCount, int currentPage, int pageSize)
            :this(dataList, totalCount,currentPage,pageSize,null)
        {
           
        }
        public PagedData(IList<T> dataList, int totalCount, int currentPage, int pageSize, string basicUrl)
        {
            this.DataList = dataList;
            this.TotalCount = totalCount;
            this.CurrentPage = currentPage;
            this.PageSize = pageSize;
            this._basicUrl = basicUrl ;
        }
        

        public PagedData(Pagination paging)
            : this(paging.Page, paging.PageSize)
        {
        }
        public PagedData(int currentPage, int pageSize)
            : this(new List<T>(), 0, currentPage, pageSize)
        {
        }
        public PagedData(int totalCount, int currentPage, int pageSize)
            : this(new List<T>(), totalCount, currentPage, pageSize)
        {
        }

        public int TotalCount { get; set; }
        public int TotalPage
        {
            get
            {
                if (this.TotalCount > 0)
                {
                    return this.TotalCount % this.PageSize == 0 ? this.TotalCount / this.PageSize : this.TotalCount / this.PageSize + 1;
                }
                else
                {
                    return 0;
                }
            }
        }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public IList<T> DataList { get; set; }
        #region vuetable2
        private string _basicUrl;

        public int per_page
        {
            get
            {
                return PageSize;
            }
            set
            {
                PageSize = value;
            }
        }
        public int current_page
        {
            get { return CurrentPage; }
            set { CurrentPage = value; }
        }
        public int last_page
        {
            get { return TotalPage; }

        }
        public string next_page_url
        {
            get
            {
                var npage = (this.current_page >= TotalPage ? TotalPage.ToString() : (current_page + 1).ToString());
                return _basicUrl + (this._basicUrl.Contains("?") ? "&page=" + npage : "?page=" + npage);
            }
        }
        public string prev_page_url
        {
            get
            {
                var npage = (this.current_page <= 1 ? "0" : (current_page - 1).ToString());
                return _basicUrl + (this._basicUrl.Contains("?") ? "&page=" + npage : "?page=" + npage);


            }
        }
        public int from
        {
            get
            {
                return current_page * PageSize + 1;
            }
        }

        public int to
        {
            get
            {
                return (current_page ) * PageSize > TotalCount
                      ? TotalCount:(current_page + 1) * PageSize - 1 ;
            }
        }
        
        /// <summary>
        /// vuetable-2 use;
        ///  total number of records available
        /// </summary>
        public int total
        {
            get
            {
                return TotalCount;
            }
            set
            {
                TotalCount = value;
            }
        }

        public string toJsonString()
        {
            throw new NotImplementedException();
        }
        #endregion

    }
}
