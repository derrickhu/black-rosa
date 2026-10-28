var InkRecommendLibrary = {
    // 辅助函数必须写在这个函数里面。微信导出只会留下注册进 LibraryManager 的函数，
    // 写在文件顶上的函数进不了框架，调用时就是 inkRecommendPage is not defined。
    InkRecommendCall: function (mode) {
        var g = typeof GameGlobal !== "undefined" ? GameGlobal : window;
        var openlink = "TWFRCqV5WeM2AkMXhKwJ03MhfPOieJfAsvXKUbWvQFQtLyyA5etMPabBehga950uzfZcH3Vi3QeEh41xRGEVFw";
        function send(msg) {
            try {
                var fn = g.Module && g.Module.SendMessage;
                if (fn) fn("WxRecommend", "OnJs", msg == null ? "" : String(msg));
            } catch (e) {}
        }
        function page() {
            if (typeof wx === "undefined" || !wx.createPageManager) return null;
            if (!g.__inkRecommendPage) {
                var created = wx.createPageManager();
                try {
                    created.on("show", function () { send("show"); });
                    created.on("destroy", function (res) {
                        send("destroy:" + (res && res.isRecommended ? "1" : "0"));
                    });
                    created.on("error", function (res) {
                        var code = res && res.errCode != null ? res.errCode : "";
                        var text = res && res.errMsg ? res.errMsg : "error";
                        send("error:" + code + " " + text);
                    });
                } catch (e) {}
                g.__inkRecommendPage = created;
            }
            return g.__inkRecommendPage;
        }
        try {
            var mgr = page();
            if (!mgr) {
                send("unsupported");
                return;
            }
            if (mode === 0) {
                var loading = mgr.load({ openlink: openlink });
                if (loading && loading.catch) loading.catch(function (e) {
                    send("error:" + (e && (e.errMsg || e.message) || e));
                });
                return;
            }
            function fail(e) {
                send("error:" + (e && (e.errMsg || e.message) || e || "fail"));
            }
            function show() {
                try {
                    var pending = mgr.show({ openlink: openlink });
                    if (pending && pending.then) pending.then(function () { send("show"); }).catch(fail);
                } catch (e) {
                    fail(e);
                }
            }
            var loaded = mgr.load({ openlink: openlink });
            if (loaded && loaded.then) loaded.then(show).catch(fail);
            else show();
        } catch (e) {
            send("error:" + (e && (e.message || e.errMsg) || e));
        }
    }
};

mergeInto(LibraryManager.library, InkRecommendLibrary);
