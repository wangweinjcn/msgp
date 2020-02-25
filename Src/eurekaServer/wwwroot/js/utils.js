var version = '1.0.1'
var ChangeDateToString = function (DateIn) {
    var Year = 0
    var Month = 0
    var Day = 0
    var CurrentDate = ''
    // 初始化时间
    Year = DateIn.getYear()
    Month = DateIn.getMonth() + 1
    Day = DateIn.getDate()
    CurrentDate = Year + '-'
    if (Month >= 10) {
        CurrentDate = CurrentDate + Month + '-'
    } else {
        CurrentDate = CurrentDate + '0' + Month + '-'
    }
    if (Day >= 10) {
        CurrentDate = CurrentDate + Day
    } else {
        CurrentDate = CurrentDate + '0' + Day
    }
    return CurrentDate
}
const storage = {
    set: function (key, value, time, type) {
        key = key + '_' + version
        var curTime = new Date().getTime()
        time = time || 7 * 24 * 60 * 60 * 1000
        value = JSON.stringify(value)
        type = type || this.storageType
        if (type) {
            sessionStorage.setItem(key, JSON.stringify({ data: value, time: curTime + time }))
        } else {
            localStorage.setItem(key, JSON.stringify({ data: value, time: curTime + time }))
        }
    },
    get: function (key, type) {
        key = key + '_' + version
        var val = ''
        if (type) {
            val = sessionStorage.getItem(key)
        } else {
            val = localStorage.getItem(key)
        }
        val = JSON.parse(val)
        if (val) {
            val.data = JSON.parse(val.data)
        }
        return val
    },
    remove: function (key, type) {
        key = key + '_' + version
        if (type) {
            sessionStorage.removeItem(key)
        } else {
            localStorage.removeItem(key)
        }
    },
    clear: function (type) {
        if (type) {
            sessionStorage.clear()
        } else {
            localStorage.clear()
        }
    }
}
const cookie = {
    set: function (key, value, time) {
        // key, '', -1 清除cookie
        document.cookie = key + '=' + escape(value) + ((time === null) ? '' : ';expires=' + time * 1000) + ';path=/'
    },
    get: function (key) {
        if (document.cookie.length > 0) {
            let start = document.cookie.indexOf(key + '=')
            if (key !== -1) {
                start = start + key.length + 1
                let end = document.cookie.indexOf(';', start)
                if (end === -1) end = document.cookie.length
                return unescape(document.cookie.substring(start, end))
            }
        }
        return ''
    }
}
