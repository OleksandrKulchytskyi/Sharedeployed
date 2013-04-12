var singletonHub = (function () {
	var instantiated;
	var msgsVM;
	var isHubStarted = false;
	var hubMessanger;

	function init() {
		// all singleton code goes here
		return {
			messanger: hubMessanger,
			startHub: function () {
				$.connection.hub.start().done(function myfunction() {
					console.log("StartHub method is being running.");
					if (singletonHub.getInstance().messanger) {
						console.log("Messanger field:" + singletonHub.getInstance().messanger);
						singletonHub.getInstance().messanger.server.join();

						console.log("msgsVM.SetMessangeR");
						msgsVM.SetMessangeR(singletonHub.getInstance().messanger);
						isHubStarted = true;
					}
				});
			},
			MsgsVM: function () {
				console.log("MsgsVM func.");
				console.log(msgsVM);
				return msgsVM;
			},
			HubStarted: isHubStarted
		}
	}

	function initViewModel() {
		console.log("initViewModel");
		return new NewMessagesViewModel();
	}

	function initHub() {
		console.log("initHub");
		return new messangerHub();
	}

	return {
		getInstance: function () {
			if (!instantiated) {
				console.log("instantiating of singletonHub.");
				msgsVM = initViewModel();
				hubMessanger = initHub();
				instantiated = init();
			}
			return instantiated;
		}
	}
})();