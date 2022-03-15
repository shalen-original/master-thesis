# HoloAssist

A Unity-based Hololens application that exposes a high-level API to draw geographically-referenced augmentations in the context of a fixed-platform flight simulator.

## Running the project

The project has been tested on Unity 2021.1.16f1 and MRTK 1.0.2104.4-Beta. [Due to stupid Windows](https://microsoft.github.io/MixedReality-WorldLockingTools-Unity/DocGen/Documentation/HowTos/WLTviaMRFeatureTool.html#when-installing-from-mixed-reality-feature-tool) and to the absurdly long path names that Unity generates, you need to place this project in a folder with a very short absolute path (less than 12 characters). The path I usually use is `C:\holo`. Yes, I don't like Windows either. You will also need to install [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity).

You should be able to run the application directly in the Unity player or to deploy it to the Hololens as any other app, see [Microsoft Hololens Tutorial](https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/unity-development-overview?tabs=arr%2CD365%2Chl2). By default, the application starts with the plane mesh hidden (click the "Toggle Mesh" button on the in-game menu to show it) and with the plane position of 0° N, 0° E, 0 meters altitude.

In order to move around the plane in the app (simulating therefore information coming from the simulator), you need the [udp-command-sender](https://gitlab.lrz.de/tulrfsd/simulation/mixed_reality_environment/udp-command-sender). After installing Rust, you can run this with "cargo run --bin sim-position-updater". Then use the keyboard keys that the application prints on screen to move around the plane. If you try moving the plane around right now, nothing will change.

In order to actually display some augmentation, download the [HoloAssist apps](https://gitlab.lrz.de/tulrfsd/simulation/mixed_reality_environment/holo-assist-apps) (the Python scripts). After the usual `pip install -r requirements.txt` you can load whatever augmentation you want. Follow the README of that repository. Of course, you need to move the location of the simulator to somewhere near the augmentation you are loading.

As a last note, you will probably need to change IP addresses, as they are currently hardcoded for this simulator.

## Reading the code

The best way is to follow the flow of UDP packets. They are received in `UDPManager.cs` and forwarded to the rest of the application. In particular, `GeoFixedExternalDrawingsManager.cs` handles most of the API messages and creates the GeoFixed meshes. `QRCodeManager.cs` and `SpacePinsManager.cs` do most of the work regarding the alignment between real and virtual world. The `Update` method of `HoloAssist.cs` updates the rotation of the plane ENU coordinate system with the information coming from the simulator. `RemoteEditor.cs` intercepts the commands to move/rotate `GameObject` via the local network.

The compilation constant `RENDER_GEOFIXED_WITH_CPU` can be defined to rended geo-fixed augmentations with the CPU in 64bit precision. By default, they are rendered on the GPU with 32bit precision.