A hack of CelesteTAS used to extract game info and send to VNyan

Instructions:

Install CelesteTAS mod via Olympus

Copy the following text:

Wind: {Level.Wind}
HairColour: {Player.Hair.Color}
CameraPos: {Level.Camera.Position}

Mod Options -> Celeste TAS -> Info HUD -> Set Custom Info Template from Clipboard

Run modified Celeste Studio.exe from this release

Monitor the console window to see which commands get sent to VNyan and write websocket handler nodes to do what you want with that info.

List of instructions, modifiable settings and example node graphs to come later. Not quite yet ready for production use as I need to do a settings screen among other things, but it'll work for some things

If you use this give me credit or a shoutout. If you somehow make millions off it, consider sending some my way :D
