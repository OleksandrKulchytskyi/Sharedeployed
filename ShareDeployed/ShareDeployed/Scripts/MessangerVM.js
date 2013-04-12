function Message(data) {
	this.Subject = ko.observable(data.Subject);
	this.From = ko.observable(data.From);
	this.When = ko.observable(data.When);
	this.Id = ko.observable(data.Id);
	this.IsRead = ko.observable(false);

	if (data.User !== undefined) {
		this.messangerUser = ko.observable(data.User.Name);
		this.userId = ko.observable(data.User.Id);
	}
}

function MessageInGroup(data, owner) {
	this.Subject = ko.observable(data.Subject);
	this.From = ko.observable(data.From);
	this.When = ko.observable(data.When);
	this.Id = ko.observable(data.Id);
	this.IsRead = ko.observable(false);

	if (data.User !== undefined) {
		this.messangerUser = ko.observable(data.User.Name);
		this.userId = ko.observable(data.User.Id);
	}
	if (owner !== null) {
		this.GroupName = ko.observable(owner);
	}
}

function GroupMessages(data) {
	this.GroupName = ko.observable(data.GroupName);
	var msgsArray = $.map(data.Messages, function (item) { return new MessageInGroup(item, data.GroupName) });
	this.Messages = ko.observableArray(msgsArray);
}

function NewMessagesViewModel() {

	var self = this;
	self.newMessages = ko.observableArray([]);
	self.groupsMessages = ko.observableArray([]);
	self.MessangeR = ko.observable(null);

	self.IsResponseOpened = ko.observable(false);
	self.MailToResponse = ko.observable(null);
	self.ResponseText = ko.observable('');

	self.SetResponseDialogState = function (state) {
		self.IsResponseOpened(state);
	};

	self.onlyNewMessages = ko.computed(function () {
		return ko.utils.arrayFilter(self.newMessages(), function (message) { return !message.IsRead() });
	});

	self.markAsRead = function (message) {
		message.IsRead(true);

		$.ajax({
			url: "/api/messanger/markAsRead?msgId=" + message.Id() + "&usrId=" + message.userId(),
			type: "GET",
			headers: { "UserId": "1", "AuthToken": "11111" },
			success: function (jsonStr) {
				self.newMessages.remove(message);
			}
		}).fail(function () {
			message.IsRead(false);
			alert('Fail to mark this item as read.');
		});
	};

	self.deleteMsg = function (msgToDelete) {
		if (self.MessangeR() !== undefined) {
			self.MessangeR().server.deleteMessage(msgToDelete.Id());
			self.groupsMessages.removeMsgFromGroup(msgToDelete.GroupName(), msgToDelete);
		}
	};

	self.onResponseToMsg = function (msgToResponse) {
		if (self.MessangeR() !== undefined) {

			if ($("#CheckMessagesDialog").dialog("isOpen") === true) {
				$("#CheckMessagesDialog").dialog("close");
			}
			self.MailToResponse(msgToResponse);
			$("#MsgResponseDialog").dialog("open");
		}
	};

	self.SendResponseToMsg = function () {
		if (self.MessangeR() !== undefined) {

			if ($("#MsgResponseDialog").dialog("isOpen") === true) {
				$("#MsgResponseDialog").dialog("close");
			}
			var text = self.ResponseText();
			self.MessangeR().server.responseToMessage(self.MailToResponse().Id(), text);
			self.ResponseText('');
		}
	};

	self.Initialize = function (mappedMsgs) {
		self.newMessages.removeAll();
		$.each(mappedMsgs, function (index, item) {
			self.newMessages.push(item);
		});
		self.newMessages.valueHasMutated();
	};

	self.initGroupsMessages = function (grpsMsgs) {
		var mappedMsgs = $.map(grpsMsgs, function (item) { return new GroupMessages(item) });
		self.groupsMessages.valueWillMutate();
		self.groupsMessages.removeAll();
		$.each(mappedMsgs, function (indx, item) {
			self.groupsMessages.push(item);
		});
		self.groupsMessages.valueHasMutated();
	};

	self.SetMessangeR = function (messangeR) {
		self.MessangeR(messangeR);
	};
}