
function updateTimeline() {
    if (window.location.href == "https://mobile.twitter.com/home"
        || window.location.href == "https://mobile.twitter.com/home/") {

        var homeButton = document.body.querySelectorAll('a[data-testid="AppTabBar_Home_Link"]');
        if (homeButton.length > 0) {
            homeButton[0].click();
        }
    }
}
