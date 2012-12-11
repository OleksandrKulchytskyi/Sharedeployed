//wrapper to an observable that requires accept/cancel
ko.protectedObservable = function (initialValue) {
    //private variables
    var _actualValue = ko.observable(initialValue),
	_tempValue = initialValue;

    //computed observable that we will return
    var result = ko.computed({
        //always return the actual value
        read: function () {
            return _actualValue();
        },
        //stored in a temporary spot until commit
        write: function (newValue) {
            _tempValue = newValue;
        }
    });

    //if different, commit temp value
    result.commit = function () {
        if (_tempValue !== _actualValue()) {
            _actualValue(_tempValue);
        }
    };

    //force subscribers to take original
    result.reset = function () {
        _actualValue.valueHasMutated();
        _tempValue = _actualValue();   //reset temp value
    };

    return result;
};

ko.protectedObservableItem = function (item) {
    for (var parameter in item) {
        if(item.hasOwnProperty(parameter))
        {
            this[parameter] = ko.protectedObservable(item[parameter]);
        }
    }

    this.commit = function () {
        for(var property in this)
        {
            this.hasOwnProperty(property) && this[property].commit)
            this[property].commit();
        }
    }
};

ko.protectedObservableItemArray = function (sourceArray) {
    var drillItems=ko.utils.arrayMap(sourceArray, function(item){
        return new ko.protectedObservableItem(item);
    });
	
    return drillItems;
};

ko.observableArray.fn.filterByProperty = function(propName, matchValue) {
    return ko.computed(function() {
        var allItems = this(), matchingItems = [];
        for (var i = 0; i < allItems.length; i++) {
            var current = allItems[i];
            if (ko.utils.unwrapObservable(current[propName]) === matchValue)
                matchingItems.push(current);
        }
        return matchingItems;
    }, this);
};

ko.observableArray.fn.pushAll = function(valuesToPush) {
    var underlyingArray = this();
    this.valueWillMutate();
    ko.utils.arrayPushAll(underlyingArray, valuesToPush);
    this.valueHasMutated();
    return this;
};

ko.observableArray.fn.sortByProperty = function(prop) {
    this.sort(function(obj1, obj2) {
        if (obj1[prop] == obj2[prop]) 
            return 0;
        else if (obj1[prop] < obj2[prop]) 
            return -1 ;
        else 
            return 1;
    });
};