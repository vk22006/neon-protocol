# Neon Protocol

A 2D top-down sci-fi survival shooter built with Godot 4.7.1 Mono and C#.

## Tech Stack

- **Engine:** Godot 4.7.1 (Mono / .NET)
- **Language:** C#
- **Genre:** Top-down shooter

## Features

### Player

- Four-direction movement using WASD / arrow keys
- Directional idle and walking animations
- Mouse-based weapon aiming with weapon flipping
- Weapon attachment system with per-direction offsets
- Weapon draw-order layering based on aim angle
- Muzzle flash animation on shooting
- Projectile firing with correct visual orientation

### Enemy

- Enemy scene with four-directional spritesheet animations (idle, move, attack, hurt)
- Player detection via Godot group system
- Configurable movement speed

### Collectibles

- Crystal artifact with spinning animation

## Project Structure

```
neon-protocol/
├── Assets/
│   ├── characters/          # Player and enemy spritesheets
│   ├── fx/                  # Muzzle flash, crystal, projectile sprites
│   └── interior/            # Tilemap textures (floors, walls, frames)
├── Scenes/
│   ├── collectibles/        # Collectible scenes
│   ├── Enemy.tscn           # Enemy scene
│   ├── Player.tscn          # Player scene
│   ├── Projectile.tscn      # Projectile scene
│   └── main.tscn            # Main game scene
├── Scripts/
│   ├── EnemyController.cs   # Enemy AI controller
│   ├── PlayerController.cs  # Player input, movement, aiming, and shooting
│   └── Projectile.cs        # Projectile movement and lifetime
└── project.godot            # Godot project configuration
```

## Controls

| Input          | Action     |
| -------------- | ---------- |
| W / Up Arrow   | Move up    |
| S / Down Arrow | Move down  |
| A / Left Arrow | Move left  |
| D / Right Arrow| Move right |
| Left Mouse / Space | Shoot  |
| Mouse          | Aim weapon |

## Scene Hierarchy (main.tscn)

```
NeonArena
├── Environment
│   ├── GroundLayer
│   ├── WallLayer
│   ├── FrameLayer
│   └── DecorationLayer
├── Actors
│   ├── Player
│   └── Enemy
├── Gameplay
└── UI
```

## Building

Requires the .NET SDK compatible with Godot 4.7.1.

```bash
dotnet build
```
