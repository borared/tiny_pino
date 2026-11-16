# Tiny Little Pino

Tiny Little Pino is a third-person obstacle course made in Unity for the CADT midterm group project. Guide Pino, a tiny plush doll, through an oversized kitchen and reach the air vent to win.

## How to play

- Pick up and stack cardboard boxes to reach higher surfaces.
- Grab a rope when the prompt appears. Climb to the height you need and release over the next platform.
- Stay near the center of narrow sticks. Stepping near the edge or jumping can make Pino slip.
- Use the switch to turn the ceiling fan on or off. Time your jumps onto and off its blades.
- Reach the vent to finish. A long fall ends the run. Choose **Try Again** to restart after a fall, or **Play Again** on the win screen to return to the start menu.

### Keyboard and mouse controls

| Input | Action |
| --- | --- |
| W, A, S, D | Move |
| Mouse | Look around |
| Space | Jump |
| F | Pick up or drop a box, toggle the fan switch, or grab or release a rope |
| E / Q | Climb up / down while holding a rope |
| Esc | Pause or resume. You can also select **Continue** in the pause menu. |

## Play the Windows or MacOS build

1. Download the ZIP from the [Google Drive](https://drive.google.com/drive/folders/136_yw8DHv8q6uCOkw6umcm1OxRdEb3Zs?usp=sharing) below.
2. Extract the whole archive into a folder. Keep the executable, its `_Data` folder, and the other build files together.
3. Open the game's `.exe` and select **Start Game**.

## Open the project in Unity

1. Install Unity **6000.3.8f1** through Unity Hub. The editor version is recorded in [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt).
2. Clone the branch that contains the playable scene.

   ```bash
   git clone --branch ketana-scripts https://github.com/borared/tiny_pino.git
   ```

3. In Unity Hub, add the cloned `tiny_pino` folder as a project and open it. Wait for Unity to import the assets.
4. In the Project window, open `Assets/Scenes/combined.unity`.
5. Press **Play** in the editor, then select **Start Game**.

## Team Members & Contributions

| Member Name | Student ID / Email | Key Contributions |
| :--- | :--- | :--- |
| **Bora Red** | `boraofficial76@gmail.com` | • Scene design, obstacle layout<br>• Shiftlock movement & procedural limb walking animations.<br>• Static rope, climbing.<br>• Camera occlusion system with sphere-casting (`MouseLook`).<br>• Audio SFX & Background Music integration (`BackgroundMusic`). |
| **Syna Sokunketana** | `synasokunketana@gmail.com` | • Core player physics & `PlayerController` implementation.<br>• Condition logic & UI setup and design (`StartMenu`) (`PauseMenu`) (`WinMenu`).<br>• Cardboard box placement and physics distribution. |
| **Tep Khengmengkhim** | `khengmengkhim.tep@student.cadt.edu.kh` | • Lighting configuration, Player loses rule (`FallDeath`) and Lose Condiction logic and UI setup & design (`LoseMenu`) |

---


## Asset and tutorial references

Model credits are recorded in the names of the imported folders under [Assets/Resources](Assets/Resources) and [Assets/Models](Assets/Models).

| Creator | Models |
| --- | --- |
| Poly by Google | Blender, boots, ceiling fan, cups, mule deer, rubber duck |
| Kenney | Kitchen cabinet drawer, fridge, stove, pot stew, toaster |
| Quaternius | Cable, cardboard boxes, cobweb, door, houseplant, spider |
| J-Toastie | Air vent, wall shelf |
| Jarlan Perez | Polaroids, stack of bowls, stack of plates |
| Isa Lousberg | Boots, oven glove |
| Zsky | Hanging flower pot, sink cabinet |
| Don Carson | Kitchen table |
| jeremy | Necktie, portrait kit |
| Kavan Bahrami | Rustic frame and window |
| Damori | Villager |

- Unity scripting references for player movement, physics objects, and camera collision. [CharacterController](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/charactercontroller), [Rigidbody](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/rigidbody), [Physics.SphereCast](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/physics/spherecast)
- The original sources for the music and sound effects, fonts, and imported images still need to be added.
- AI assistance was used to revise this README.

## Project links

| Deliverable | Link |
| --- | --- |
| Source repository | [Tiny Little Pino on GitHub](https://github.com/borared/tiny_pino) |
| Gameplay video | [https://www.youtube.com/watch?v=teZDpx8qWY4](https://www.youtube.com/watch?v=teZDpx8qWY4) |
| Playable Windows build | [Google Drive](https://drive.google.com/drive/folders/136_yw8DHv8q6uCOkw6umcm1OxRdEb3Zs?usp=sharing) |
