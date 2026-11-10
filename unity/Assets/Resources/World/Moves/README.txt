Mixamo baseball moves (free Adobe account, https://www.mixamo.com). Put the FBX files here with these names:
pitch.fbx  swing.fbx  stance.fbx  catch.fbx  throw.fbx  run.fbx  pickup.fbx  cheer.fbx
Download settings: Format FBX for Unity, Skin: Without Skin, 30 fps, Keyframe Reduction none. Tick "In Place" where offered.
The editor imports them as humanoid and switches the player models to humanoid by itself (Editor/ArtImport.cs).
A missing file just falls back to the models' own clips.
