A Skeleton Unity3D (2018.2) project
===================================

This is a Unity3D project meant to be used at the Aalto University NEPPI course.

Current version (30th Aug 2018) runs only on OSX. Also a commerctial AQUAS package is used, but not included here.


As this project uses
[Git submodules](https://git-scm.com/book/en/v2/Git-Tools-Submodules),
to use this project skeleton, please make sure that you have the
appropriate submodules initialised.

Command Line Setup
==================

```
   $ git clone --recurse-submodules git@github.com:AaltoNEPPI/Unity_NEPPI_Skeleton.git
```

Github tool setup
=================

Please someone write instructions here and submit a pull request.

Features
========

At this point this example project supports a simple built-in
web-server.  For a (temporary) example, see the WebServer Game Object
in the SampleScene Scene, in the `Assets/Scenes` folder.

The web server is able to serve Framework7 based content, simply
by adding the Framework7Component script to any game object.  The
SampleScene has also the Framework7Component.

Once you have the webserver running, you can access your local
Framework7 kitchen sink at
```
   http://localhost:8079/fw7/kitchen-sink/index.html
```

Planned features
================

Make the Server tell you a non-localhost URL to use for access.
Add a BLE component.
Run the Web server as a background object, independent of game
objects.
