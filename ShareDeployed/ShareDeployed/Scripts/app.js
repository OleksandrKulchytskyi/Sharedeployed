//var appViewModel = new
//    {
//        MainVM: function () {
//            var self = this;

//            this.authToken = ko.observable("");
//            this.userId = ko.observable(0);
//        },
//        ExpenseVM: {},
//        RevenueVM: {},
//        Init: function () {
//            alert('Yep we did it');
//        }
//    };

var system = {};

// Models
system.checkCookies = function (navigaror) {
	var cookieEnabled = (navigator.cookieEnabled) ? true : false;
	
	if (typeof navigator.cookieEnabled == "undefined" && !cookieEnabled) {
		document.cookie = "testcookie";
		cookieEnabled = (document.cookie.indexOf("testcookie") != -1) ? true : false;
	}
	return (cookieEnabled);
}