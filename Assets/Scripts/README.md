# Simplified Game Scripts

This folder contains the essential C# scripts for the GMTK 2025 game project.

## Folder Structure

```
Scripts/
├── Components/          # Game component scripts
│   ├── PlayerController.cs    # Handles player movement, jumping, and health
│   ├── SimpleHealthBar.cs     # UI health bar component
│   └── SimpleDamage.cs        # Damage dealing component
└── README.md           # This file
```

## Components

### PlayerController
Handles all player functionality in one simple component.

**Features:**
- Basic movement with WASD/Arrow keys
- Simple jumping with spacebar
- Health system with damage taking
- Ground detection

**Setup:**
1. Add to player GameObject
2. Ensure GameObject has Rigidbody2D component
3. Set moveSpeed and jumpForce in inspector

### SimpleHealthBar
Displays player health as a UI slider.

**Features:**
- Automatically finds PlayerController
- Updates health bar in real-time
- Works with Unity UI Slider component

**Setup:**
1. Add to UI GameObject with Slider component
2. Assign PlayerController reference (optional - will auto-find)

### SimpleDamage
Deals damage to the player on collision or trigger.

**Features:**
- Configurable damage amount
- Collision or trigger damage options
- Simple and lightweight

**Setup:**
1. Add to enemy/hazard GameObject
2. Set damage amount in inspector
3. Choose collision or trigger mode

## Usage

1. **Player Setup:**
   - Create a GameObject for the player
   - Add Rigidbody2D component
   - Add PlayerController component
   - Add Collider2D for collision detection

2. **Health Bar Setup:**
   - Create UI Slider in Canvas
   - Add SimpleHealthBar component
   - Player reference will be found automatically

3. **Enemy/Hazard Setup:**
   - Create GameObject for enemy/hazard
   - Add Collider2D (set as trigger if needed)
   - Add SimpleDamage component
   - Set damage amount

## Controls

- **Movement:** WASD or Arrow Keys
- **Jump:** Spacebar
- **Health:** Automatically managed by PlayerController

This simplified structure removes all unnecessary complexity while maintaining core game functionality. 