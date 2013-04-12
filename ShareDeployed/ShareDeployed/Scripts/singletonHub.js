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
					if (singletonHub.getInstance().messanger) {

						singletonHub.getInstance().messanger.server.join();
						msgsVM.SetMessangeR(singletonHub.getInstance().messanger);
						isHubStarted = true;
					}
				});
			},
			MsgsVM: function () {
				return msgsVM;
			},
			HubStarted: isHubStarted
		}
	}

	function initViewModel() {
		return new NewMessagesViewModel();
	}

	function initHub() {
		return new messangerHub();
	}

	return {
		getInstance: function () {
			if (!instantiated) {
				msgsVM = initViewModel();
				hubMessanger = initHub();
				instantiated = init();
			}
			return instantiated;
		}
	}
})();