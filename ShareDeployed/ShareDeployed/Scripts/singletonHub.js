var singletonHub = (function () {
	var instantiated;
	function init() {
		// all singleton code goes here
		return {
			messanger: new messangerHub()
		}
	}

	return {
		getInstance: function () {
			if (!instantiated) {
				instantiated = init();
			}
			return instantiated;
		}
	}
})();