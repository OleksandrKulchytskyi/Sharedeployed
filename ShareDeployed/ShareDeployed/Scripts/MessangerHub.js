function messangerHub() {

	var messanger = $.connection.messangerHub;

	messanger.client.onMessage = function (message) { };

	messanger.client.onStatus = function (status) {
		var count = $("#connectionStatuses").children().length;
		if (count === 3) {
			$("#connectionStatuses").empty();
		}
		$('#connectionStatuses').append('<li> ' + status + '</li>');
	};

	messanger.client.markInactive = function (userVMList) {
		console.log(userVMList);
	};

	messanger.client.broadcastMessages = function (grpName, messages) {
		if (singletonHub.getInstance().MsgsVM().IsResponseOpened() === true) {
			return;
		}

		var mappedMsgs = $.map(messages, function (item) { return new Message(item) });

		singletonHub.getInstance().MsgsVM().Initialize(mappedMsgs);

		if ($("#NewMessagesDialogDiv").dialog("isOpen") === true) {
			$("#NewMessagesDialogDiv").dialog("close");
		}

		var msgsCount = 'You have ' + messages.length.toString() + ' new message(s), Group:' + grpName;
		$("#NewMessagesDialogDiv").dialog({ title: msgsCount });
		$("#NewMessagesDialogDiv").dialog('open');
	};

	messanger.client.OnNewMessagesResponse = function (data) {
		singletonHub.getInstance().MsgsVM().initGroupsMessages(data);

		if ($("#CheckMessagesDialog").dialog("isOpen") === true) {
			$("#CheckMessagesDialog").dialog("close");
		}

		$("#CheckMessagesDialog").dialog({ title: "All messages" });
		$("#CheckMessagesDialog").dialog('open');
	};

	$.connection.hub.connectionSlow(function () {
		var count = $("#connectionStatuses").children().length;
		if (count == 3) {
			$("#connectionStatuses").empty();
		}
		$('#connectionStatuses').append('<li>There seems to be some connectivity issues...</li>');
	});

	return messanger;
};