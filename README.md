## A hack of CelesteTAS used to extract game info and send to VNyan for your model to react to

### Features
* Track Madeline's hair colour (e.g. to make your model's hair follow Madeline's)
    * Supports hair colour mods (tested with Hyperline)
    * Supports extra dash mods (tested with Pointless Machines in Strawberry Jam)
* Track in-game wind (e.g. make VNyan wind follow game wind and blow your own hair to match what is on-screen)
* Track death & respawn (e.g. ragdoll your model when you die)
* Calculate if Madeline is behind your VTuber (e.g. hide or move your model so viewers can see what you're doing)
* Track various other events (e.g. swimming, feather use, bubble use and more)

### Instructions

1. Download from: https://github.com/LumKitty/CelesteTAV/releases/latest
2. Install CelesteTAS mod via Olympus Mod Manager: https://gamebanana.com/tools/6449
3. Copy the following text:
```
Wind: {Level.Wind}
HairColour: {Player.Hair.Color}
CameraPos: {Level.Camera.Position}
```
4. Launch Celeste, go to Mod Options -> Celeste TAS -> Info HUD -> Set Custom Info Template from Clipboard
5. (Optional): Unbind all keyboard and controller bindings for CelesteTAS
6. Run modified Celeste Studio.exe from this release after the game has reached the title screen
7. (Optional): Import the example node graph into VNyan to get a starter config
8. Monitor the console window to see which commands get sent to VNyan and write websocket handler nodes to do what you want with that info.
  
### Websocket messages 

| **CelesteFeather** | 1 = feather is active, 0 = exited feather |
| **CelesteDead** | 1 = dead, 0 = respawned |
| **CelesteSwim** | 1 = entered water, 0 = left water |
| **CelesteBubble** | 1 = in a bubble, (can't tell if red or green) 0 = left bubble |
| **CelesteRedBubbleDash** | 1 = red bubble doing its thing 0 = left bubble |
| **CelesteMadelineBehindVTuber** | 1 = behind, 0 = moved away again (see instructions below) |
| **CelesteBadelineLaunch** | 1 = Start of being thrown off screen, 0 = off screen about to scene change |
| **CelesteIntroJump** | 1 = fired immediately on scene change after a Badeline launch: 0 = player has control back |
| **CelesteWind** | 3 comma separated values: X direction, Y direction and windspeed \
X and Y are intended to be used in a Vector3 to give the direction, but they are raw values. The wind node doesn't care about the intensity, it just calculates a direction. Windspeed needs to be set as well and is simply the square root of x^2 + y^2. Values are typically 0-1200. Max on the VNyan wind node is 10, so I recommend dividing by 100 |
| **CelesteHairColour** | is Madeline's current hair colour in a format readable by the "Text to Color" node (#RRGGBBAA). |
| **CelesteRoom** | The name of the current room you're in |
| **CelesteMenu** | text parameter with many possible values, recommend watching the console as you navigate menus if you want to work with this one |

Config file only needs to be changed if you need to change the VNyan URL from the default ws://127.0.0.1:8000/vnyan or you need to tweak the MadelineBehindVTuber option.\

### Configuring detection of Madeline behind VTuber

Co-ordinates are based on the Celeste window (X: 0 - 320, Y: 0 - 180, starting in the bottom left). You need to define a "Danger Zone" square as close to where your model will be as possible, and a "Safe Zone" square that is bigger.\
When Madeline enters the danger zone we send CelesteMadelineBehindVTuber 1 and when she leaves the safe zone we send a 0. You could then use this to e.g. hide your VTuber or make them transparent so that viewers can still see what you're doing. The reason safe zone needs to be bigger is to avoid the situation where you're constantly appearing and disappearing with only very small movements (e.g. maintaining position on windy levels)

### Configuring hair colour changing

This is beyond the scope of this document but you need the following:
1. Hair should be white and must be using the Poiyomi shader: https://github.com/poiyomi/PoiyomiToonShader
2. Hair material should be set to animated when locked
3. You must have Jayo's Poiyomi plugin for VNyan installed and understand how to use it: https://github.com/jayo-exe/JayoPoiyomiPlugin \
In the example node graph the material for the hair is called "CelesteHairWhite"


If you use this mod, maybe give me a shoutout or something. If you somehow make millions off it, consider sending some my way :D 
(Also send me a link to your stream, I'd love to see what people come up with using this!)

https://twitch.tv/LumKitty
