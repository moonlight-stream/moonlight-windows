#Moonlight

####Update 7/1/2015: Development is happening again with the release of the new Windows 10 APIs. We don't know yet where the project will land, but we're working to bring game streaming to your Windows devices! 

####Moonlight Windows development is on hold until Windows apps support real time streaming of raw h264. We will provide any updates if this changes. We're sorry for the inconvenience and hope that we can get this up and running as soon as possible.

Note: Moonlight Windows is in development and is not considered stable. 

Moonlight is an open source implementation of NVIDIA's GameStream, as used by the NVIDIA Shield.
We reverse-engineered the Shield streaming software, and created a version that can be run on any Windows or Windows Phone 8.1 device. Moonlight Windows is built on the Windows Runtime (WinRT). 

Moonlight will allow you to stream your full collection of games from your PC to your Windows or Windows Phone device,
in your own home, or over the internet.

##Features

* Streams any of your Steam games from your PC to your Windows device
* Automatically finds GameStream-compatible PCs on your network

##Features in development
* Keyboard and Controller support

##Installation

* Moonlight Windows is in development and not yet available for free download in the Store. If you want to try it in the meantime, download the source and deploy the app to your phone. 
* Download [GeForce Experience](http://www.geforce.com/geforce-experience) and install on your Windows PC

##Requirements

* [GameStream compatible](http://shield.nvidia.com/play-pc-games/) computer with GTX 600/700 series GPU
* PC, tablet, or phone running Windows 8.1 or higher. 

##Usage

* Turn on GameStream in the GFE settings
* If you are connecting from outside the same network, turn on internet
  streaming
* When on the same network as your PC, open Moonlight and tap on your PC in the list and then tap "Pair With PC"
* Accept the pairing confirmation on your PC
* Play games!

##Contribute 
- Fork us and set up a solution in Visual Studio
- Add [Moonlight Common](https://github.com/moonlight-stream/moonlight-common-c) as a project in your solution
- Write code
- Send Pull Requests

##Authors

* [Michelle Bergeron](https://github.com/mrb113)
* [Cameron Gutman](https://github.com/cgutman)

##Other Official Versions:
[Moonlight](https://github.com/moonlight-stream) also has an Android
implementation, and versions for PC and iOS are currently in development.
