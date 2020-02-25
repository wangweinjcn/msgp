/* 异步请求结果返回状态码 */
var ResultStatus = { OK: 100, Failed: 101, NotLogin: 102, Unauthorized: 103 };
/* $vmpa */
(function ($) {
    var $vmpa = {};
    $vmpa.axios = axios.create();
    $vmpa.addOrChangeToTab = function (tabUrl, tabDDid, tabTitle) {
        var needadd = false;
        var element = top.layui.element;
        if (top.$(".layui-tab-title li[lay-id]").length <= 0) {
            //如果比零小，则直接打开新的tab项
            needadd = true;
        } else {
            //否则判断该tab项是否以及存在

            needadd = true;
            $.each(top.$(".layui-tab-title li[lay-id]"), function () {
                //如果点击左侧菜单栏所传入的id 在右侧tab项中的lay-id属性可以找到，则说明该tab项已经打开
                if (top.$(this).attr("lay-id") === tabDDid) {
                    needadd = false; //找到就不需要增加
                }
            })          
        }
        if (needadd === true) {
            //标志为false 新增一个tab项
            //新增一个Tab项 传入三个参数，分别对应其标题，tab页面的地址，还有一个规定的id，是标签中data-id的属性值
            //关于tabAdd的方法所传入的参数可看layui的开发文档中基础方法部分
            element.tabAdd('content', {
                title: tabTitle
                , content: '<iframe data-frameid="' + tabDDid + '" scrolling="auto" frameborder="0" src="' + tabUrl + '" style="width:100%;height:99%;"></iframe>' //支持传入html
                , id: tabDDid
            });
            $vmpa.FrameWH(); 
        }
        element.tabChange('content', tabDDid);
    }
    $vmpa.FrameWH = function () {
        var h = top.innerHeight - 41 - 10 - 60 - 10 - 44 - 10;
        $(top.$("iframe")[top.$("iframe").length -1]).css("height", h + "px");
    }
    /* 返回 json 数据 */
    $vmpa.get = function (url, data, callback) {
        if ($.isFunction(data)) {
            callback = data;
            data = undefined;
        }

        url = parseUrl(url);
        var ret = execAjax("GET", url, data, callback);
        return ret;
    }
    $vmpa.post = function (url, data, callback) {
        if ($.isFunction(data)) {
            callback = data;
            data = undefined;
        }

        var ret = execAjax("POST", url, data, callback);
        return ret;
    }
    $vmpa.postSync = function (url, data, callback) {
        if ($.isFunction(data)) {
            callback = data;
            data = undefined;
        }

        var ret = execAjaxSync("POST", url, data, callback);
        return ret;
    }
    $vmpa.alert = function (msg, callBack) {
        layerAlert(msg, callBack);
    }
    $vmpa.confirm = function (msg, callBack) {
        layerConfirm(msg, callBack);
    }
    $vmpa.msg = function (msg) {
        layerMsg(msg);
    }

    $vmpa.reload = function () {
        location.reload();
        return false;
    }

    /* 将当前 url 的参数值 */
    $vmpa.getQueryParam = function (name) {
        if (name === null || name === undefined || name === "")
            return "";
        name = ("" + name).toLowerCase();
        var search = location.search.slice(1);
        var arr = search.split("&");
        for (var i = 0; i < arr.length; i++) {
            var ar = arr[i].split("=");
            if (ar[0].toLowerCase() === name) {
                if (unescape(ar[1]) === 'undefined') {
                    return "";
                } else {
                    return unescape(ar[1]);
                }
            }
        }
        return "";
    }
    /* 将当前 url 参数转成一个 js 对象 */
    $vmpa.getQueryParams = function () {
        var params = {};
        var loc = window.location;
        var se = decodeURIComponent(loc.search);

        if (!se) {
            return params;
        }

        var paramsString;
        paramsString = se.substr(1);//将?去掉
        var arr = paramsString.split("&");
        for (var i = 0; i < arr.length; i++) {
            var item = arr[i];
            var index = item.indexOf("=");
            if (index === -1)
                continue;
            var paramName = item.substr(0, index);
            if (!paramName)
                continue;
            var value = item.substr(index + 1);
            params[paramName] = value;
        }
        return params;
    }

    /* optionList: [{"Value" : "1", "Text" : "开发部"},{"Value" : "2", "Text" : "测试部"}] */
    $vmpa.getOptionTextByValue = function (optionList, value, valuePropName, textPropName) {
        valuePropName = valuePropName || "Value";
        textPropName = textPropName || "Text";

        var text = "";
        var len = optionList.length;
        for (var i = 0; i < len; i++) {
            if (optionList[i][valuePropName] === value) {
                text = optionList[i][textPropName];
                break;
            }
        }

        return text;
    }


    $vmpa.formatBool = function (val) {
        if (val === true) {
            return "是";
        }
        else if (val === false) {
            return "否";
        }

        return val;
    }
    $vmpa.postJsonWithJwt = function (url, data, callback) {
        var layerIndex = layer.load(1);
        var ret = $.ajax({
            url: url,
            beforeSend: function (request) {
                var _jwttoken = cookie.get('jwtToken')
                if (_jwttoken !== null)
                    request.setRequestHeader("Authorization", 'Bearer ' + _jwttoken);
            },
            type: "POST",
            dataType: "json",
            contentType: "application/json",
            data: JSON.stringify(data),
            complete: function (xhr) {
                layer.close(layerIndex);
            },
            success: function (result) {
                var isStandardResult = ("Status" in result) && ("Msg" in result);
                if (isStandardResult) {
                    if (result.Status === ResultStatus.Failed) {
                        layerAlert(result.Msg || "操作失败");
                        callback(result);
                        return;
                    }
                    if (result.Status === ResultStatus.NotLogin) {
                        layerAlert(result.Msg || "未登录或登录过期，请重新登录");
                        return;
                    }
                    if (result.Status === ResultStatus.Unauthorized) {
                        layerAlert(result.Msg || "权限不足，禁止访问");
                        return;
                    }

                    if (result.Status === ResultStatus.OK) {
                        /* 传 result，用 result.Data 还是 result.Msg，由调用者决定 */
                        callback(result);
                    }
                }
                else
                    callback(result);
            },
            error: errorCallback
        });
        return ret;
    }

    $vmpa.postJson = function (url, data, callback) {
        var layerIndex = layer.load(1);
        console.log(data)
        var ret = $.ajax({
            url: url,
            type: "POST",
            dataType: "json",
            contentType: "application/json",
            data: JSON.stringify(data),
            complete: function (xhr) {
                layer.close(layerIndex);
            },
            success: function (result) {
                var isStandardResult = ("Status" in result) && ("Msg" in result);
                if (isStandardResult) {
                    if (result.Status === ResultStatus.Failed) {
                        layerAlert(result.Msg || "操作失败");
                        callback(result);
                        return;
                    }
                    if (result.Status === ResultStatus.NotLogin) {
                        layerAlert(result.Msg || "未登录或登录过期，请重新登录");
                        return;
                    }
                    if (result.Status === ResultStatus.Unauthorized) {
                        layerAlert(result.Msg || "权限不足，禁止访问");
                        return;
                    }

                    if (result.Status === ResultStatus.OK) {
                        /* 传 result，用 result.Data 还是 result.Msg，由调用者决定 */
                        callback(result);
                    }
                }
                else
                    callback(result);
            },
            error: errorCallback
        });
        return ret;
    }
    function execAjaxWithJwt(type, url, data, callback) {
        var layerIndex = layer.load(1);
        var ret = $.ajax({
            url: url,
            beforeSend: function (request) {
                var _jwttoken = cookie.get('jwtToken')
                if (_jwttoken !== null)
                    request.setRequestHeader("Authorization", 'Bearer ' + _jwttoken);
            },
            type: type,
            dataType: "json",
            data: data,
            complete: function (xhr) {
                layer.close(layerIndex);
            },
            success: function (result) {
                var isStandardResult = ("Status" in result) && ("Msg" in result);
                if (isStandardResult) {
                    if (result.Status === ResultStatus.Failed) {
                        layerAlert(result.Msg || "操作失败");
                        return;
                    }
                    if (result.Status === ResultStatus.NotLogin) {
                        layerAlert(result.Msg || "未登录或登录过期，请重新登录");
                        return;
                    }
                    if (result.Status === ResultStatus.Unauthorized) {
                        layerAlert(result.Msg || "权限不足，禁止访问");
                        return;
                    }

                    if (result.Status === ResultStatus.OK) {
                        /* 传 result，用 result.Data 还是 result.Msg，由调用者决定 */
                        callback(result);
                    }
                }
                else
                    callback(result);
            },
            error: errorCallback
        });
        return ret;
    }
    function execAjaxSync(type, url, data, callback) {
        var layerIndex = layer.load(1);
        var ret = $.ajax({
            url: url,
            type: type,
            async: false, 
            dataType: "json",
            data: data,
            complete: function (xhr) {
                layer.close(layerIndex);
            },
            success: function (result) {
                var isStandardResult = ("Status" in result) && ("Msg" in result);
                if (isStandardResult) {
                    if (result.Status === ResultStatus.Failed) {
                        layerAlert(result.Msg || "操作失败");
                        return;
                    }
                    if (result.Status === ResultStatus.NotLogin) {
                        layerAlert(result.Msg || "未登录或登录过期，请重新登录");
                        return;
                    }
                    if (result.Status === ResultStatus.Unauthorized) {
                        layerAlert(result.Msg || "权限不足，禁止访问");
                        return;
                    }

                    if (result.Status === ResultStatus.OK) {
                        /* 传 result，用 result.Data 还是 result.Msg，由调用者决定 */
                        callback(result);
                    }
                }
                else
                    callback(result);
            },
            error: errorCallback
        });
        return ret;
    }
    function execAjax(type, url, data, callback) {
        var layerIndex = layer.load(1);
        var ret = $.ajax({
            url: url,
            type: type,
            dataType: "json",
            data: data,
            complete: function (xhr) {
                layer.close(layerIndex);
            },
            success: function (result) {
                var isStandardResult = ("Status" in result) && ("Msg" in result);
                if (isStandardResult) {
                    if (result.Status === ResultStatus.Failed) {
                        layerAlert(result.Msg || "操作失败");
                        return;
                    }
                    if (result.Status === ResultStatus.NotLogin) {
                        layerAlert(result.Msg || "未登录或登录过期，请重新登录");
                        return;
                    }
                    if (result.Status === ResultStatus.Unauthorized) {
                        layerAlert(result.Msg || "权限不足，禁止访问");
                        return;
                    }

                    if (result.Status === ResultStatus.OK) {
                        /* 传 result，用 result.Data 还是 result.Msg，由调用者决定 */
                        callback(result);
                    }
                }
                else
                    callback(result);
            },
            error: errorCallback
        });
        return ret;
    }
    function errorCallback(xhr, textStatus, errorThrown) {
        var json = { textStatus: textStatus, errorThrown: errorThrown };
        alert("请求失败: " + JSON.stringify(json));
    }
    function parseUrl(url) {
        if (url.indexOf("_dc=") < 0) {
            if (url.indexOf("?") >= 0) {
                url = url + "&_dc=" + (new Date().getTime());
            } else {
                url = url + "?_dc=" + (new Date().getTime());
            }
        }
        return url;
    };

    function layerAlert(msg, callBack) {
        msg = msg === null ? "" : msg;/* layer.alert 传 null 会报错 */
        var type = 'warning';
        var icon = "";
        if (type === 'success') {
            icon = "fa-check-circle";
        }
        if (type === 'error') {
            icon = "fa-times-circle";
        }
        if (type === 'warning') {
            icon = "fa-exclamation-circle";
        }

        var idx;
        idx = layer.alert(msg, {
            icon: icon,
            title: "系统提示",
            btn: ['确认'],
            btnclass: ['btn btn-primary'],
        }, function () {
            layer.close(idx);
            if (callBack)
                callBack();
        });
    }
    function layerConfirm(content, callBack) {
        var idx;
        idx = layer.confirm(content, {
            icon: "fa-exclamation-circle",
            title: "系统提示",
            btn: ['确认', '取消'],
            btnclass: ['btn btn-primary', 'btn btn-danger'],
        }, function () {
            layer.close(idx);
            callBack();
        }, function () {

        });
    }
    function layerMsg(msg, callBack) {
        msg = msg === null ? "" : msg;/* layer.msg 传 null 会报错 */
        layer.msg(msg, { time: 2000, shift: 0 });
    }


    window.$vmpa = $vmpa;
})($);

function ViewModelBase() {
    var me = this;
    me.init = function (idname) {
        var app = new Vue({
            el: '#app',
            data: function () {
                return vmd.data;
            },
            methods: vmd.methods,
            created: function () {
                this.getMainTableData();
                this.OnCreate();
            },
            computed: vmd.computed,
        })
    }
   
    me.data = {
        mainDialog: null,
        slaveDialog: null,
        mainSelectedModel: null,
        mainCommandDisnable: true,
        slaveSelectedModel: null,
        slaveCommandDisnable:true,

        pageSize: 10,

        mainPageIndex: 1,
        mainTotalCount: -1,
        mainSearchModel: { "keyword": null },

        mainTableConfig: {
            multipleSort: false,
            tableData: [],
            columns: []
        },
        slavePageIndex: 1,
        slaveTotalCount: -1,
        slaveSearchModel: { "keyword": null },

        slaveTableConfig: {
            multipleSort: false,
            tableData: [],
            columns: []
        }
    }
    me.computed= {
        _maincommandDisnable:function(){
            if (me.data.mainSelectedModel === null)
                return true;
            else
                return false;
        }
    }
    me.methods = {
        OnCreate : function () {
        },
        /* 搜索数据逻辑，子类根据需要重写 */
        getMainTableData: function () {

            var data = me.data.mainSearchModel;
            data.Page = me.data.mainPageIndex;
            data.PageSize = me.data.pageSize;
            $vmpa.get(me.mainLoadTablePagedDataUrl, data, function (result) {
                me.data.mainTableConfig.tableData = result.Data.DataList;
                me.data.pageSize = result.Data.PageSize;
                me.data.mainPageIndex = result.Data.CurrentPage;
                me.data.mainTotalCount = result.Data.TotalCount;
            }
            )

        },
        mainPageChange: function (mainPageIndex) {

            this.mainPageIndex = mainPageIndex;
            this.getMainTableData();
        },
        mainPageSizeChange: function (pageSize) {
            ;

            this.mainPageIndex = 1;
            this.pageSize = pageSize;
            this.getMainTableData();
        },

        mainRowClick: function (rowData) {
            me.data.mainSelectedModel = this.mainTableConfig.tableData[rowData];
            me.data.mainCommandDisnable = false;
            console.log('mainRowClick', me.data.mainSelectedModel);
        },
        /* 搜索按钮点击事件 */
        mainSearch: function () {
            this.getMainTableData();
        },

        /* 删除按钮点击事件 */
        mainDelete: function () {
            $vmpa.confirm("确定要删除该条数据吗？", me.OnMainDelete);
        },
        mainAdd: function () {
            var newmodel = { Id: -1 };

            me.OnMainEdit(newmodel, "新增");
        },
        mainEdit: function () {

            me.OnMainEdit(me.data.mainSelectedModel, "编辑");
        },
        mainRefresh: function () {

            me.getMainTableData();
        },
        getSlaveTableData: function () {

            var data = me.data.slaveSearchModel;
            data.Page = me.data.slavePageIndex;
            data.PageSize = me.data.pageSize;
            data.MainId = me.data.mainSelectedModel[me.ModelKeyName];
            $vmpa.get(me.slaveLoadTablePagedDataUrl, data, function (result) {
                me.data.slaveTableConfig.tableData = result.Data.DataList;
                me.data.pageSize = result.Data.PageSize;
                me.data.slavePageIndex = result.Data.CurrentPage;
                me.data.slaveTotalCount = result.Data.slaveTotalCount;

            }
            )

        },
        slavePageChange: function (slavePageIndex) {

            this.slavePageIndex = slavePageIndex;
            this.getSlaveTableData();
            console.log(slavePageIndex)
        },
        slavePageSizeChange: function (pageSize) {
            ;

            this.slavePageIndex = 1;
            this.pageSize = pageSize;
            this.getSlaveTableData();
        },

        slaveRowClick: function (rowData) {
            me.data.slaveSelectedModel = this.slaveTableConfig.tableData[rowData];
            me.data.slaveCommandDisnable = false;
            console.log('slaveRowClick', this.slaveTableConfig.tableData[rowData]);
        },
        /* 搜索按钮点击事件 */
        slaveSearch: function () {
            this.getSlaveTableData();
        },

        /* 删除按钮点击事件 */
        slaveDelete: function () {
            $vmpa.confirm("确定要删除该条数据吗？", me.OnSlaveDelete);
        },
        slaveAdd: function () {
            var newmodel = { Id: -1 };

            me.OnSlaveEdit(newmodel, "新增");
        },
        slaveEdit: function () {

            me.OnSlaveEdit(me.data.slaveSelectedModel, "编辑");
        },
        EnsureNotNull: function (obj, name) {
            if (!obj)
                throw new Error("属性 " + name + " 未初始化");
        }

    }
    me.mainClassName = null;
    me.mainColCountPerRow = 2;/*编辑form上每行放置数据列数，标题和编辑器算一列*/
  

    me.ModelKeyName = "Id"; /* 实体主键名称 */

    me.mainLoadTablePagedDataUrl = null; /*获取主表格数据地址，分页*/
    me.mainAddUrl = null;/*添加主表格对象地址*/
    me.mainUpdateUrl = null;/*更新主表格对象地址*/
    me.mainDeleteUrl = null;/*删除主表格对象地址*/
    me.mainDetailUrl = null;/*获取主表格详情地址*/

    me.slaveLoadTablePagedDataUrl = null; /*获取从表格数据地址，分页*/
    me.slaveAddUrl = null;/*添加从表格对象地址*/
    me.slaveUpdateUrl = null;/*更新从表格对象地址*/
    me.slaveDeleteUrl = null;/*删除从表格对象地址*/
    me.slaveDetailUrl = null;/*获取从表格详情地址*/

   
    me.OnMainEdit = function (model, title) {
        if (me.data.mainDialog !== null) {
            me.data.mainDialog.Open(model, title);
        }
    };
    me.OnMainDelete = function () {
        me.DeleteMainRow();
    };
    /* 要求每行必须有 Id 属性，如果主键名不是 Id，则需要重写 me.ModelKeyName */
    me.DeleteMainRow = function () {
        if (me.mainDeleteUrl === null)
            throw new Error("未指定 mainDeleteUrl");

        var url = me.mainDeleteUrl + "/" + me.data.mainSelectedModel[me.ModelKeyName];
        var params = {};
        $vmpa.post(url, params, function (result) {
            var msg = result.Msg || "删除成功";
            $vmpa.msg(msg);
            var index = me.data.mainTableConfig.tableData.indexOf(me.data.mainSelectedModel);
            if (index > -1) {
                me.data.mainTableConfig.tableData.splice(index, 1);
            }
           
            me.data.mainSelectedModel = null;

        });
    };
    me.OnSlaveDelete = function () {
        me.DeleteSlaveRow();
    };
    /* 要求每行必须有 Id 属性，如果主键名不是 Id，则需要重写 me.ModelKeyName */
    me.DeleteSlaveRow = function () {
        if (me.slaveDeleteUrl === null)
            throw new Error("未指定 slaveDeleteUrl");

        var url = me.slaveDeleteUrl + "/" + me.data.slaveSelectedModel[me.ModelKeyName];
        var params = {};
        $vmpa.post(url, params, function (result) {
            var msg = result.Msg || "删除成功";
            $vmpa.msg(msg);
            var index = me.data.slaveTableConfig.tableData.indexOf(me.data.slaveSelectedModel);
            if (index > -1) {
                me.data.slaveTableConfig.tableData.splice(index, 1);
            }
            me.data.slaveSelectedModel = null;
        });
    };
    me.OnSlaveEdit = function (model, title) {
        if (me.data.slaveDialog !== null) {
            me.data.slaveDialog.Open(model, title);
        }
    };

}

function DialogBase(vm, _isMain) {

    var me = this;
    if (!_isMain)
        me.isMain = false;
    else
        me.isMain = true;
    me._vm = vm;
    me.Title = null;
    me.IsShow = false;
    me.loading = true;
    me.EditModel = {};
    
    me.Close = function () {
        me.IsShow = false;
    }
    me.Open = function (model, title) {
        me.IsShow = true;
        me.EditModel = model;


        if (title)
            me.Title = title;

        me.OnOpen();
    }
    me.Save = function () {
        me.OnSave();
        
        me.Close();
    }
    me.refreshTable = function () {
        if (me.isMain)
            me._vm.methods.getMainTableData();
        else
            me._vm.methods.getSlaveTableData();
    }


    me.OnOpen = function () {
        var detailUrl = "";
        if (me.isMain)
            detailUrl = me._vm.mainDetailUrl;
        else
            detailUrl = me._vm.slaveDetailUrl;
        if (detailUrl !== "" && me.EditModel.Id>0) {
            $vmpa.get(detailUrl + "/" + me.EditModel.Id, null, function (result) {
                if (result.Status === ResultStatus.OK) {
                    me.EditModel = result.Data;
                }
            });
        }
    }
    me.OnSave = function (model) {
        var updateurl = "";
        if (me.EditModel[me._vm.ModelKeyName] > 0) {


            if (me.isMain)
                updateurl = me._vm.mainUpdateUrl;
            else
                updateurl = me._vm.slaveUpdateUrl;
            $vmpa.post(updateurl, me.EditModel, function (result) {
                me.refreshTable();
            })
        } else {
            if (me.isMain)
                updateurl = me._vm.mainAddUrl;
            else
                updateurl = me._vm.slaveAddUrl;
            $vmpa.post(updateurl, me.EditModel, function (result) {
                me.refreshTable();
            })

        }

    }
}




