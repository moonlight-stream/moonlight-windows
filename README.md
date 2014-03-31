#Limelight

Note: Limelight Windows Phone is in development and is not considered stable. 

#####What is it? 
Limelight is an open source implementation of NVIDIA's GameStream, as used by the NVIDIA Shield,
but built for Windows Phone 8. 

#####What does this mean for you? 
Limelight allows you to stream your full collection of Steam games from your GFE compatible PC to any of your supported devices.

This app streams games from a GameStream-compatible PC on your local network. After initial pairing (not yet implemented in Limelight WP), Limelight will launch and stream the Steam Big-Picture interface where you can launch any game that has been added to Steam. Video will be streamed back to your Windows Phone device. Mouse input is sent from your Windows Phone to the PC.

To have a good experience, you need a mid to high-end wireless router (preferably dual-band 802.11n or better) with a good wireless connection to your Windows Phone.

#####PC Requirements:
- NVIDIA GeForce GTX 600/700/800 series GPU running the primary display
- NVIDIA GeForce Experience 1.7 or later
- Steam enrolled in beta updates
- Solid connection to wireless router (Ethernet recommended)

If you are running recent GeForce drivers (334.xx and 335.xx) or your video stops for several seconds while streaming, you should roll back to 332.21. There is a driver bug that affects the official SHIELD streaming app and Limelight (and possibly other software using NVENC).

#####Windows Phone Requirements: 
- Windows Phone 8

#####Other Official Versions:

[Limelight](https://github.com/limelight-stream) also has an Android
implementation, and versions for PC and iOS are currently in development.
