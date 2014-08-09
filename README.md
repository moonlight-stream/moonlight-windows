#Limelight

Note: Limelight Windows Phone is in development and is not considered stable. 

Limelight is an open source implementation of NVIDIA's GameStream, as used by the NVIDIA Shield.
We reverse engineered the Shield streaming software, and created a version that can be run on any Windows Phone device.

Limelight will allow you to stream your full collection of games from your Windows PC to your Windows Phone device,
in your own home, or over the internet.

##Features

* Streams any of your games from your PC to your Android device
* Automatically finds GameStream-compatible PCs on your network

##Features in development
* Video and Audio rendering
* MOGA controller support

##Installation

* Limelight Windows Phone is in development and not yet available for free download in the Store. If you want to try it in the meantime, download the source and deploy the app to your phone. 
* Download [GeForce Experience](http://www.geforce.com/geforce-experience) and install on your Windows PC

##Requirements

* [GameStream compatible](http://shield.nvidia.com/play-pc-games/) computer with GTX 600/700 series GPU
* Windows Phone device running Windows Phone 8 or higher

##Usage

* Turn on GameStream in the GFE settings
* If you are connecting from outside the same network, turn on internet
  streaming
* When on the same network as your PC, open Limelight and tap on your PC in the list and then tap "Pair With PC"
* Accept the pairing confirmation on your PC
* Tap your PC again to view the list of apps to stream
* Play games!

##Contribute: 
- Fork us and set up a solution in Visual Studio
- Add [Limelight Common](https://github.com/limelight-stream/limelight-common-c) as a project in your solution
- Write code
- Send Pull Requests

##Authors

* [Michelle Bergeron](https://github.com/mrb113)
* [Cameron Gutman](https://github.com/cgutman)

##Other Official Versions:
[Limelight](https://github.com/limelight-stream) also has an Android
implementation, and versions for PC and iOS are currently in development.
