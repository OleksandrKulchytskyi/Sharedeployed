/*
* Author: Oleksandr Kulchytskyi
* Date: 30.11.2012
* Description: MessangeR namespace js file and viewmodels declaration
*/

var pageView = {
	newMessagesView: null
}

$(document).ready(function () {
	page.initialize();
	page.hookupEvents();
});

page.getEmptyMessage = function () {
	var obj =
    {
    	Subject: "",
    	From: "",
    	When: 1970,
    }
	return obj;
}

pageView.newMessagesDialog = function () {
	$el = $("#NewMessagesDialogDiv");
	$el.show().draggable()
		.closable().centerInClient({ centerOnceOnly: true })

	// bind with empty data
	var data = page.getEmptyMessage();

	// map to ko view model
	if (!page.albumEditView) {
		albumEditView = ko.mapping.fromJS(data);
		ko.applyBindings(albumEditView, $("#divAddAlbumDialog")[0]);
		//page.editalbumFirstBind = false;
	}
	else
		ko.mapping.fromJS(data, albumEditView);
}

// Namespace
var messenger = {};

// Models
messanger.chatMessage = function (sender, content, dateSent) {
	var self = this;
	self.username = sender;
	self.content = content;
	if (dateSent != null) {
		self.timestamp = dateSent;
	}
}

messanger.user = function (username, connectionId) {
	var self = this;
	self.username = username;
	self.id = connectionId;
}

// ViewModels
messanger.chatViewModel = function () {
	var self = this;
	self.messages = ko.observableArray();
}

messanger.connectedUsersViewModel = function () {
	var self = this;
	self.contacts = ko.observableArray();
	self.customRemove = function (userToRemove) {
		var userIdToRemove = userToRemove.id;
		self.contacts.remove(function (item) {
			return item.id === userIdToRemove;
		});
	}
}