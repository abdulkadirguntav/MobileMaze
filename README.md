# 🏃 Project: E.C.H.O (Test Subject Zero)

![Unity](https://img.shields.io/badge/Unity-6-black?style=for-the-badge&logo=unity)
![C#](https://img.shields.io/badge/C%23-Programming-blue?style=for-the-badge&logo=c-sharp)
![Platform](https://img.shields.io/badge/Platform-Android-green?style=for-the-badge&logo=android)
![Status](https://img.shields.io/badge/Status-In_Development-orange?style=for-the-badge)

> *"When you realize you are in a simulation, the only thing left to do is break the rules."*


**Project: E.C.H.O** is a fast-paced, 3-lane 3D Endless Runner mobile game where you control an advanced prototype trying to escape a colorful and deadly simulation run by an AI. It's not just about dodging left and right; it's a fight for survival using gravity-defying acrobatic moves.

![0406-ezgif com-crop](https://github.com/user-attachments/assets/36f06eef-2597-4c2a-9510-66f1cf842b8d)


## 🎮 Gameplay Features

In addition to classic mobile runner mechanics, dynamic physics mechanics that reward player reflexes are integrated:

* **Smooth 3-Lane System:** Seamless and fluid transitions between lanes using Swipe controls and `Vector3.Lerp`.
* **Zero-G Wall-Run:** The player locks onto walls on suitable surfaces with gravity disabled. This is the only way to bypass impassable lasers and obstacles.
* **Fast-Fall & Slide:** When a downward swipe command is given mid-air, the character instantly drops to the ground (Fast-Fall) and transitions into a sliding animation, shrinking its hitbox.
* **Risk & Reward (Close Call):** Players who dodge obstacles milliseconds before impact are rewarded with extra points.

## 🛠️ Under the Hood

This project is designed targeting maximum performance (60 FPS) on mobile platforms:

* **Dynamic Level Generator (Chunk Spawner):** Modular path chunks are generated in a random yet logical order to provide an endless tunnel feeling.
* **Object Pooling:** Instead of constantly creating new objects (Instantiate/Destroy), passed path chunks and obstacles are sent back to the pool, ensuring RAM-friendly memory management (Garbage Collection Optimization).
* **State-Driven Animations:** The character's rotation and physical state (on a wall, in the air) are manipulated in real-time via code. Mixamo animations are blended with custom Rigidbody physics.

## 🎨 Art Design and Atmosphere

The game's lore is built on an "AI Test Simulation". Therefore, instead of gloomy environments, a colorful, low-poly, and vibrant open-air environment was chosen. 
* *Visual Assets: Kenney & Quaternius (CC0 License)*

## 🚀 Installation

To open and inspect the project in Unity:
1. Clone the repository: `git clone https://github.com/[YourUsername]/Project-Echo.git`
2. Select the folder by clicking `Add Project` via Unity Hub.
3. Open the `MainLevel` scene under the `Scenes` folder.
4. Select Android or iOS device format via Unity Simulator and press Play.

## 🗺️ Roadmap

- [x] Coding the core Swipe and 3-Lane system
- [x] Jump, Slide, and Fast-Fall mechanics
- [x] Wall-Run physics manipulation
- [x] Chunk Spawner (Endless Level Generator) integration
- [ ] Designing the low-poly colorful simulation environment (WIP)
- [ ] Setting up the obstacle pool and difficulty curve
- [ ] Collectibles and UI integration
- [ ] Audio (BGM and SFX) implementation

## 👨‍💻 Developer

**Abdülkadir Güntav**
