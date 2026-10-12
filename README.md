# Shark Hunger

2.5D Hungry Shark-style survival game. Unity 6000.3.25f1 (Unity 6), built-in render pipeline, low-poly look with all
meshes and materials generated in code. Gameplay runs on a 2D side-scrolling plane, with a perspective 3D camera,
seafloor, seaweed, light rays and bubbles. Targets: WebGL (and macOS).

**Play:** https://mikenev.github.io/shark-hunger/ · **Post:** https://mikenev.github.io/blog/shark-hunger/

## Gameplay
The shark doesn't grow. It has to keep eating to survive. A hunger bar drains all the time, faster the longer you
survive and faster again while boosting. When it hits zero the game is over. Creatures:

- **Sardines** swim in schools and scatter when you get close
- **Clownfish** are slow and easy to catch
- **Tuna** flee, so you have to boost to catch them
- **Sea turtles** are slow and give a big hunger refill
- **Jellyfish** sting and take hunger away
- **Pufferfish** are good food unless they're puffed up, then they hurt
- **Crabs** scuttle along the seafloor and run away
- **Golden fish** are rare and fast, worth 250 points

## Controls
WASD / arrow keys to swim, or hold the mouse / touch to steer toward the pointer. Space or Shift to boost.
Space / Enter / R to start or restart.

## Run it
Open the project in Unity 6000.3.25f1 and open `Assets/Scenes/Main.unity`. Press Play.

## Rebuild
`Shark Hunger > Setup Project` regenerates the materials, the Main scene and the player/WebGL settings
(`Assets/Editor/SharkBuild.cs`). The scene only holds a `GameBootstrap` object; everything else is built at runtime.

Headless WebGL build (output in `Builds/WebGL`, gitignored):

```
Unity -batchmode -quit -projectPath . -executeMethod SharkBuild.BuildWebGL
```

`SharkBuild.BuildMac` builds a macOS app to `Builds/Mac/`. Serve the WebGL build over HTTP (for example
`cd Builds/WebGL && python3 -m http.server 8080`); opening `index.html` from disk won't load.

## Layout
```
Assets/
  Editor/      SharkBuild (project setup + WebGL/Mac builds)
  Scenes/      Main.unity
  Resources/Materials/   Opaque, Transparent, Additive base materials
  Scripts/
    GameBootstrap, GameManager (hunger/score/game states + HUD), World, Spawner
    SharkController, CameraFollow, Creature, CreatureFactory
    OceanEnvironment, Shapes (procedural meshes), Mats, Fx (particles + sound)
```

## Publish
The live build is in the site repo at `mikenev/mikenev.github.io` under `public/shark-hunger/`. To update it, rebuild
WebGL and copy `Builds/WebGL/*` there. GitHub Pages is intentionally **not** enabled on this repo, so it doesn't
take over the `/shark-hunger/` path.
