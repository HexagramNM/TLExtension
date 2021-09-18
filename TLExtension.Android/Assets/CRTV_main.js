function showCitedRTLink() {
    var currentURL = window.location.href;
    var re = /^https:\/\/mobile.twitter.com\/([a-z]|[A-Z]|[0-9]|_)*\/status\/[0-9]*$/;
    var isTweetWindow = re.test(currentURL);
    if (isTweetWindow) {
        var previousTarget = document.getElementsByClassName("css-1dbjc4n r-1k8odwz");
        if (previousTarget.length > 0) {
            var target = previousTarget[0].parentNode;
            var targetNum = target.childNodes.length;
            var targetPoint = target.childNodes[targetNum - 1];
            var dontHasDivCRTVResult = (document.getElementById("CRTVResult") == null);
            if (dontHasDivCRTVResult) {
                var citedRTTitle = document.createElement("h3");
                citedRTTitle.innerHTML = "引用RTを表示";
                citedRTTitle.id = "CRTVNumIndicator";
                var linkCRTV = document.createElement("a");
                linkCRTV.className = "CRTVLink";
                linkCRTV.href = "https://mobile.twitter.com/search?q=" + currentURL.replace("mobile.", "") + "&f=live";
                var divCRTVResult = document.createElement("div");
                divCRTVResult.className = "CRTVDivLink";
                divCRTVResult.id = "CRTVResult";
                linkCRTV.prepend(citedRTTitle);
                divCRTVResult.prepend(linkCRTV);
                target.insertBefore(divCRTVResult,targetPoint);
            }
        }
    }
    var reSearch = /^https:\/\/mobile.twitter.com\/search\?q=https(:\/\/|\%3(A|a)\%2(F|f)\%2(F|f))twitter.com(\/|\%2(F|f))([a-z]|[A-Z]|[0-9]|_)*(\/|\%2(F|f))status(\/|\%2(F|f))[0-9]*/;
    var isSearchWindow = reSearch.test(currentURL);
    var reRTWithComment = /^https:\/\/mobile.twitter.com\/([a-z]|[A-Z]|[0-9]|_)*\/status\/([0-9])*\/retweets\/with_comments$/;
    var isRTWithCommentWindow = reRTWithComment.test(currentURL);
    if (isSearchWindow || isRTWithCommentWindow) {
        var citedRTClass = "css-1dbjc4n r-1ets6dv r-1867qdf r-rs99b7 r-1loqt21 r-wa8dpy r-1ny4l3l r-1udh08x r-o7ynqc r-6416eg";
        var citedRT = document.getElementsByClassName(citedRTClass);
        for (var idx = 1; idx < citedRT.length; idx++) {
            citedRT[idx].remove();
        }
    }
}
