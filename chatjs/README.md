ChatJS
======

[ChatJS](http://www.chatjs.net/) is a full-featured, lightweight, Facebook style jQuery plugin for instant messaging.
ChatJS is divided into two parts: `jquery.chatjs.js`, which is the actual plug-in, and an adapter, which implements the communication between the client and the server. ChatJS comes with an adapter for ASP.NET SignalR, and another one using long-polling, which is intended to be server independent, that is, it will work as long as you implement the proper server-side endpoints. ChatJS sample project works with both adapters.

Dependencies
-------------------

ChatJS depends on [jQuery](http://jquery.com/) and two jQuery plugins, [Autosize](http://www.jacklmoore.com/autosize/) and [Activity-indicator](http://neteye.github.io/activity-indicator.html):
```js
<script src="/Scripts/jquery-1.8.1.min.js" type="text/javascript"></script>
<script src="/ChatJs/Scripts/jquery.autosize.min.js" type="text/javascript"></script>
<script src="/ChatJs/Scripts/jquery.activity-indicator-1.0.0.min.js" type="text/javascript"></script>
```

About the adapter
-----------------------


* [Understanding the internals of a ChatJS adapter](https://github.com/andrerpena/chatjs/wiki/The-anatomy-of-a-ChatJS-adapter)
* [Getting started with SignalR](https://github.com/andrerpena/chatjs/wiki/Getting-up-and-running-with-SignalR)
* [Getting started with long polling](https://github.com/andrerpena/chatjs/wiki/Getting-up-and-running-with-long-polling)
