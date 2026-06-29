# Quake Movement Controller for Unity 6

A fast-paced first-person movement controller inspired by **Quake**, **Source Engine**, and modern movement shooters.

The project focuses on responsive controls, momentum-based movement, and clean, modular code architecture.

## Features

* 🚶 Walking
* 🏃 Sprinting with stamina system
* 🪂 Air acceleration
* ⚡ Dash mechanic
* 🦘 Coyote Time
* 🎯 Jump Buffering
* 🧱 Wall Jump
* 🛝 Momentum-based Slide
* 🪑 Dynamic Crouching
* 🎥 Smooth camera crouch transition
* 🧠 Source/Quake-style acceleration
* 🛑 Counter movement for precise control
* 🧱 CharacterController-based collision

## Movement Mechanics

### Walking & Running

* Responsive ground movement
* Momentum bonus while continuously moving
* Sprint speed controlled by stamina

### Air Movement

* Quake-inspired air acceleration
* Full directional air control
* Preserves player momentum

### Slide

* Enter slide while sprinting
* Steering without gaining extra speed
* Friction-based deceleration
* Automatically transitions into crouch

### Crouch

* Dynamic CharacterController resizing
* Camera smoothly lowers during crouch
* Prevents standing up under obstacles

### Jump System

* Ground jump
* Air jump
* Coyote Time
* Jump Buffer
* Wall Jump with directional reflection

### Dash

* Directional dash
* Supports forward, backward and sideways dashes
* Uses stamina

## Technologies

* Unity 6
* C#
* Unity Input System
* CharacterController

## Project Structure

```
PlayerInputHandler
        │
        ▼
PlayerMovement
        │
        ▼
PlayerMotor
        │
        ▼
CharacterController
```

## Design Goals

This controller is designed to provide:

* responsive controls
* readable and maintainable code
* modular architecture
* classic arena shooter movement
* easy extensibility for future mechanics

## Planned Features

* Bunny Hop
* Surf physics
* Wall Run
* Ledge Grab
* Vault
* Grappling Hook
* Camera Effects
* Multiplayer support

## Controls

| Action         | Key        |
| -------------- | ---------- |
| Move           | WASD       |
| Look           | Mouse      |
| Jump           | Space      |
| Sprint         | Left Shift |
| Crouch / Slide | Left Ctrl  |
| Dash           | Q          |

## License

This project is released under the MIT License.
