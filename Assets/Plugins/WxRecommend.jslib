var InkRecommendOpenLink = "TWFRCqV5WeM2AkMXhKwJ03MhfPOieJfAsvXKUbWvQFQtLyyA5etMPabBehga950uzfZcH3Vi3QeEh41xRGEVFw";

function inkRecommendSend(msg) {
    try {
        var send = GameGlobal.Module && GameGlobal.Module.SendMessage;
        if (send) send("WxRecommend", "OnJs", msg == null ? "" : String(msg));
    } catch (e) {}
}

function inkRecommendPage() {
    if (typeof wx === "undefined" || !wx.createPageManager) return null;
    var g = GameGlobal;
    if (!g.__inkRecommendPage) {
        var page = wx.createPageManager();
        try {
            page.on("show", function () { inkRecommendSend("show"); });
            page.on("destroy", function (res) {
                inkRecommendSend("destroy:" + (res && res.isRecommended ? "1" : "0"));
            });
            page.on("error", function (res) {
                var code = res && res.errCode != null ? res.errCode : "";
                var text = res && res.errMsg ? res.errMsg : "error";
                inkRecommendSend("error:" + code + " " + text);
            });
        } catch (e) {}
        g.__inkRecommendPage = page;
    }
    return g.__inkRecommendPage;
}

var InkRecommendLibrary = {
    InkRecommendLoad: function () {
        var page = inkRecommendPage();
        if (!page) {
            inkRecommendSend("unsupported");
            return;
        }
        try {
            var pending = page.load({ openlink: InkRecommendOpenLink });
            if (pending && pending.catch) pending.catch(function (e) {
                inkRecommendSend("error:" + (e && (e.errMsg || e.message) || e));
            });
        } catch (e) {
            inkRecommendSend("error:" + (e && e.message || e));
        }
    },
    InkRecommendShow: function () {
        var page = inkRecommendPage();
        if (!page) {
            inkRecommendSend("unsupported");
            return;
        }
        function fail(e) {
            inkRecommendSend("error:" + (e && (e.errMsg || e.message) || e || "fail"));
        }
        function show() {
            try {
                var pending = page.show({ openlink: InkRecommendOpenLink });
                if (pending && pending.then) pending.then(function () { inkRecommendSend("show"); }).catch(fail);
            } catch (e) {
                fail(e);
            }
        }
        try {
            var loaded = page.load({ openlink: InkRecommendOpenLink });
            if (loaded && loaded.then) loaded.then(show).catch(fail);
            else show();
        } catch (e) {
            fail(e);
        }
    }
};

mergeInto(LibraryManager.library, InkRecommendLibrary);
