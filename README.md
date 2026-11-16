# Tiny Little Pino 🧸

> **Mid-Term Group Project — Playable Obstacle Course**  
> **Course:** Game Design & Development (Weeks 1–5)  
> **Department:** Department of Computer Sciences, Institute of Digital Technology, Cambodia Academy of Digital Technology (CADT)

---

## 📌 1. Game Overview & Title
* **Game Title:** Tiny Little Pino
* **Genre:** 3D Physics-based Platformer & Puzzle Obstacle Course
* **Target Platform:** Windows Standalone (PC)
* **Unity Version:** `6000.3.8f1` (Unity 6)

### 🎯 Objective
You play as **Pino**, a tiny plush doll lost in an oversized kitchen and living space. Your goal is to traverse a challenging obstacle course across kitchen counters, hanging ropes, a ceiling fan, precarious balance sticks, and cardboard boxes to reach the designated goal zone to win and escape.

### 📜 Game Rules
1. **Find & Stack Boxes:** Explore the room to find cardboard boxes and carry them to build staircases and bridges to higher platforms.
2. **Precision Movement on Sticks:** Walking across thin sticks and narrow beams requires careful, slow movement. Rushing, running at full speed, or jumping off-center causes Pino to lose balance, slip off the rounded edge, and fall to the ground.
3. **Rope Swinging & Climbing:** Grab dangling ropes to swing across gaps. Climb up or down while holding onto the rope to position your jump.
4. **Interactive Environment:** Use switches to toggle ceiling fans on or off. Jump on rotating fan blades to reach high-altitude countertops.
5. **Win Condition:** Successfully navigate the obstacles and reach the designated goal to trigger the Win Menu!

---

## 🎮 2. Controls & How to Play

| Action | Control (Keyboard & Mouse) | Description |
| :--- | :--- | :--- |
| **Move** | <kbd>W</kbd> <kbd>A</kbd> <kbd>S</kbd> <kbd>D</kbd> | Move character in 3D space |
| **Look / Rotate Camera** | <kbd>Mouse</kbd> | Orbit 3rd-person camera around Pino |
| **Jump** | <kbd>Space</kbd> | Jump off the ground |
| **Interact / Pick Up / Drop** | <kbd>F</kbd> | Pick up nearby boxes, drop carried boxes, or toggle light switches |
| **Climb Rope Up** | <kbd>E</kbd> | Climb upwards while holding a rope |
| **Climb Rope Down** | <kbd>Q</kbd> | Slide downwards while holding a rope |
| **Release Rope** | <kbd>F</kbd> | Detach from the rope to launch into the air |
| **Restart / Retry** | Menu Button | Restart the game from the pause or win menu |

---

## ⚙️ 3. Setup and Run Instructions

### Playing the Standalone Build (Without Unity Editor)
1. Download the ZIP file from the [Playable Build Link](#-6-project-links--deliverables) below.
2. Extract the ZIP archive onto your computer (ensure all `_Data` folders and executables stay together in the same directory).
3. Double-click `tiny_pino.exe` to launch the game.
4. Click **Start Game** on the main menu to begin playing!

### Opening the Project in Unity Editor
1. Install **Unity 6** (version `6000.3.8f1` or later recommended).
2. Clone or download the repository:
   ```bash
   git clone https://github.com/borared/tiny_pino.git
   ```
3. Open Unity Hub, click **Add project from disk**, and select the `tiny_pino` folder.
4. Open the primary scene located at:
   `Assets/Scenes/combined.unity`
5. Press the **Play** button at the top of the Unity Editor.

---

## 👥 4. Team Members & Contributions

| Member Name | Student ID / Email | Key Contributions |
| :--- | :--- | :--- |
| **Bora Red** | `boraofficial76@gmail.com` | • Core player physics & `PlayerController` implementation.<br>• Non-shiftlock free movement & procedural limb walking animations.<br>• Dynamic rope physics, climbing, and swing momentum (`RopeMoment`).<br>• Camera occlusion system with smart sphere-casting (`MouseLook`).<br>• Slippery stick balance deflection physics (`SlipperyStick`).<br>• Audio SFX & Background Music integration (`BackgroundMusic`). |
| **Syna Sokunketana** | `synasokunketana@gmail.com` | • Scene design, obstacle layout, and lighting configuration.<br>• Win condition logic & UI setup (`WinMenuManager`).<br>• Cardboard box placement and physics distribution.<br>• Menu UI design, button hover effects, and fade transitions. |
| **Tep Khengmengkhim** | `khengmengkhim.tep@student.cadt.edu.kh` | • Repository initialization and project setup.<br>• Level testing, collision tuning, and gameplay verification.<br>• Asset sourcing and documentation. |

---

## 🛠️ 5. Asset & Tutorial References

### 3D Models & Props
* **Kitchen & Household Props:** Poly by Google, Kenney Asset Packs (Furniture/Kitchen Kit), Quaternius (Houseplant, Spider, Cobwebs), Jarlan Perez (Stack of Plates, Stack of Bowls, Polaroids).
* **Character Mesh:** Custom doll mesh "Pino" with procedural procedural limb hierarchy.
* **Textures & Materials:** Kenney textures, custom procedural shaders, and Unity Standard/Lit materials.

### Audio & Sound Effects
* **Background Music (BGM):** `bgm_pino.mp3` (Calm ambient atmospheric track).
* **Sound Effects (SFX):**
  * Box Pickup / Drop SFX (`pick up box.mp3`)
  * Switch Click SFX (`fan_switch.mp3`)
  * Ceiling Fan Hum SFX (`fan_sound.mp3`)

### Tutorials & Resources
* Unity Documentation on `CharacterController`, `Physics.SphereCast`, and `Rigidbody` constraints.
* Unity EventSystem & TextMeshPro UI styling techniques.
* Git branching, merge workflows, and collaborative project management best practices.

---

## 🔗 6. Project Links & Deliverables

* **GitHub Repository:** [https://github.com/borared/tiny_pino](https://github.com/borared/tiny_pino)
* **Gameplay Video Recording (YouTube / Drive):** `[INSERT YOUR YOUTUBE OR GOOGLE DRIVE LINK HERE]`
* **Playable Standalone Build (ZIP Download):** `[INSERT YOUR GOOGLE DRIVE OR GITHUB RELEASES LINK HERE]`

---

*Developed for the CADT Mid-Term Group Project (Playable Obstacle Course).*

