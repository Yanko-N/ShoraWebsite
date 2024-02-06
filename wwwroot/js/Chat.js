"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/Accounts/Chat").build();

//Disable the send button until connection is established.
document.getElementById("sendButton").disabled = true;

connection.on("ReceiveMessage", function (user,mensagemId, message) {
    var li = document.createElement("li");
    var messagesList = document.getElementById("messagesList");

    if (!messagesList) {
        console.error("Element with id 'messagesList' not found.");
        return;
    }
    connection.invoke("CheckIfIsVista", user, mensagemId);
    messagesList.appendChild(li);
    var userNameParts = user.split('-');
    var userNamePrefix = userNameParts.length > 0 ? userNameParts[0] : message.user.userName;
    li.innerHTML = message.isAdmin ? `<strong>SHORA:</strong> : ${message.text}` : `<strong>${userNamePrefix}:</strong> : ${message.text}`;


    messagesList.scrollTop = messagesList.scrollHeight;


});

connection.on("ReceiveChatHistory", function (chatHistory) {

    var messagesList = document.getElementById("messagesList");

    if (!messagesList) {
        console.error("Element with id 'messagesList' not found.");
        return;
    }
    messagesList.innerHTML = "";

    chatHistory.forEach(function (message) {
        var li = document.createElement("li");
        document.getElementById("messagesList").appendChild(li);
        var userNameParts = message.user.userName.split('-');
        var userNamePrefix = userNameParts.length > 0 ? userNameParts[0] : message.user.userName;
        li.innerHTML = message.isAdmin ? `<strong>SHORA:</strong> : ${message.text}` : `<strong>${userNamePrefix}:</strong> : ${message.text}`;



    });

    messagesList.scrollTop = messagesList.scrollHeight;
});

connection.start().then(function () {
    var reservaId = document.getElementById("reservaInput").value;
    var userName = document.getElementById("userInput").value;


    connection.invoke("AskChatHistory", reservaId, userName).catch(function (err) {
        return console.error(err.toString());
    });

    connection.invoke("AddToGroup", reservaId).catch(function (err) {
        return console.error(err.toString());
    });

    document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});


document.getElementById("sendButton").addEventListener("click", function () {
    SendMessage()
});

document.getElementById("messageInput").addEventListener("keydown", function (event) {
    if (event.key === "Enter") {
        SendMessage();
    }
});

function SendMessage() {
    var reservaId = document.getElementById("reservaInput").value;
    var user = document.getElementById("userInput").value;
    var message = document.getElementById("messageInput").value;

    connection.invoke("SendMessage", reservaId, user, message).catch(function (err) {
        return console.error(err.toString());
    });

    document.getElementById("messageInput").value = "";

}
