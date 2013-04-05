var singletonHub = (function () {
	var instantiated;
	var messagnger;

	function init() {
		// all singleton code goes here
		return {
			messanger: new messangerHub(),
			startHub: function () {
				$.connection.hub.start().done(function myfunction() {
					console.log("StartHub method is being running.");
					if (singletonHub.getInstance().messanger) {
						console.log("Messanger field:" + singletonHub.getInstance().messanger);
						singletonHub.getInstance().messanger.server.join();
					}
				});
			},
			initViewModel: function (MsgsVM) {
				console.log(MsgsVM);
				MsgsVM.SetMessangeR(singletonHub.getInstance().messanger);
			}
		}
	}

	return {
		getInstance: function () {
			if (!instantiated) {
				console.log("instantiating of singletonHub.");
				instantiated = init();
			}
			return instantiated;
		}
	}
})();