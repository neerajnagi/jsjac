/**
 * ChatJS 1.0 - MIT License
 * www.chatjs.net
 * 
 * Copyright (c) 2013, André Pena
 * All rights reserved.
 *
 **/

// More about adapters: https://github.com/andrerpena/chatjs/wiki/The-anatomy-of-a-ChatJS-adapter
// About the SignalR adapter: https://github.com/andrerpena/chatjs/wiki/Getting-up-and-running-with-SignalR

function SignalRAdapter() {
    /// <summary>
    /// SignalR adapter for ChatJS. In order to use this adapter.. Pass an instance of this 
    /// function to $.chat()
    /// </summary>
}

SignalRAdapter.prototype = {
    init: function (chat, done) {
        /// <summary>This function will be called by ChatJs to initialize the adapter</summary>
        /// <param FullName="chat" type="Object"></param>
        var _this = this;


        // These are the methods that ARE CALLED BY THE SERVER.
        // Client functions should not call these functions.
        _this.hub = $.connection.chatHub;
        _this.hub.client.sendMessage = function (message) {
            chat.client.sendMessage(message);
        };

        _this.hub.client.sendTypingSignal = function (otherUserId) {
            chat.client.sendTypingSignal(otherUserId);
        };

        _this.hub.client.usersListChanged = function (usersList) {
            chat.client.usersListChanged(usersList);
        };

        if (!window.hubReady)
            window.hubReady = $.connection.hub.start();

        window.hubReady.done(function () {
            // function passed by ChatJS to the adapter to be called when the adapter initialization is completed
            done();
        });

        // These are the methods that ARE CALLED BY THE CLIENT
        // Client functions should call these functions
        _this.server = new Object();

        _this.server.sendMessage = function (otherUserId, messageText, clientGuid, done) {
            /// <summary>Sends a message to server</summary>
            /// <param FullName="otherUserId" type="Number">The id of the user to which the message is being sent</param>
            /// <param FullName="messageText" type="String">Message text</param>
            /// <param FullName="clientGuid" type="String">Message client guid. Each message must have a client id in order for it to be recognized when it comes back from the server</param>
            /// <param FullName="done" type="Function">Function to be called when this method completes</param>
            _this.hub.server.sendMessage(otherUserId, messageText, clientGuid).done(function (result) {
                if (done)
                    done(result);
            });
        };

        _this.server.sendTypingSignal = function (otherUserId, done) {
            /// <summary>Sends a typing signal to the server</summary>
            /// <param FullName="otherUserId" type="Number">The id of the user to which the typing signal is being sent</param>
            /// <param FullName="done" type="Function">Function to be called when this method completes</param>
            _this.hub.server.sendTypingSignal(otherUserId).done(function (result) {
                if (done)
                    done(result);
            });
        };

        _this.server.getMessageHistory = function (otherUserId, done) {
            /// <summary>Gets message history from the server</summary>
            /// <param FullName="otherUserId" type="Number">The id of the user from which you want the history</param>
            /// <param FullName="done" type="Number">Function to be called when this method completes</param>
            _this.hub.server.getMessageHistory(otherUserId).done(function (result) {
                if (done)
                    done(result);
            });
        };

        _this.server.getUserInfo = function (otherUserId, done) {
            /// <summary>Gets information about the user</summary>
            /// <param FullName="otherUserId" type="Number">The id of the user from which you want the information</param>
            /// <param FullName="done" type="Function">FUnction to be called when this method completes</param>
            _this.hub.server.getUserInfo(otherUserId).done(function (result) {
                if (done)
                    done(result);
            });
        };
        
        _this.server.getUsersList = function (done) {
            /// <summary>Gets the list of the users in the current room</summary>
            /// <param FullName="otherUserId" type="Number">The id of the user from which you want the information</param>
            /// <param FullName="done" type="Function">FUnction to be called when this method completes</param>
            _this.hub.server.getUsersList().done(function (result) {
                if (done)
                    done(result);
            });
        };
    }
}