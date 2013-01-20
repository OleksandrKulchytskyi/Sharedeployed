var SingletonMessangeR = new function SingletonMessangeR() {
	var instance = this;

	SingletonMessangeR.getInstance = function () {
		return instance;
	}

	var messangerHub;
}