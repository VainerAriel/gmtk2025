# Components Documentation

This directory contains various Unity components for the GMTK 2025 game.

## Pressure Plate System

The pressure plate system consists of two main components:

### PressurePlate.cs
A trigger-based component that detects when the player or ghost stands on it and activates a door movement system.

**Setup Instructions:**
1. Create a GameObject for the pressure plate
2. Add a SpriteRenderer component (optional, for visual feedback)
3. Add the PressurePlate script
4. The script will automatically add the necessary colliders:
   - A solid BoxCollider2D for the player to stand on
   - A trigger BoxCollider2D for detection (as a child object)
5. Assign the door GameObject in the inspector
6. Configure the movement direction, distance, and speed

**Inspector Settings:**
- `Door Object`: The GameObject that will move when the plate is activated
- `Move Direction`: Vector2 direction the door should move (e.g., Vector2.right for horizontal movement)
- `Move Distance`: How far the door moves in grid units
- `Move Speed`: Speed of door movement
- `Player Layer`: Layer mask for player detection
- `Add Solid Collider`: Automatically add a solid collider for the player to stand on
- `Add Trigger Collider`: Automatically add a trigger collider for detection
- `Plate Renderer`: SpriteRenderer for visual feedback (optional)
- `Activated Color`: Color when plate is activated (default: green)
- `Deactivated Color`: Color when plate is deactivated (default: red)

**Plate Movement Settings:**
- `Move Plate Down`: Whether the pressure plate moves down when activated
- `Plate Move Distance`: How far the plate moves down (in grid units)
- `Plate Move Speed`: Speed of plate movement

**Ghost Integration:**
- Pressure plates can be activated by both players and ghosts
- The plate stays activated as long as any entity (player or ghost) is standing on it
- Multiple entities can be on the plate simultaneously
- The plate deactivates only when all entities have left

**Plate Movement:**
- When activated, the pressure plate moves down into the ground
- When deactivated, the pressure plate moves back up to its original position
- Movement is smooth and configurable
- Plate resets to original position on player respawn

### DoorController.cs
Handles the visibility and collision of doors when activated by pressure plates. When activated, the door becomes invisible and passable (no movement).

**Setup Instructions:**
1. Create a GameObject for the door
2. Add the DoorController script
3. The script will automatically be configured by the PressurePlate component

**Features:**
- Door becomes invisible and passable when activated
- Disables both sprite rendering and collision detection
- No movement - door stays in place and only changes visibility
- Automatic reset functionality for respawn systems
- State tracking (open/closed)
- Simple disappear/reappear behavior

**Public Methods:**
- `Activate()`: Hides the door and makes it passable
- `Deactivate()`: Shows the door and makes it solid again
- `Reset()`: Returns door to original position and makes it visible/solid
- `IsOpen()`: Returns current open state

**Inspector Settings:**
- `Door Sprite`: SpriteRenderer component (auto-assigned if not set)
- `Door Collider`: Collider2D component (auto-assigned if not set)
- `Door Rigidbody`: Rigidbody2D component (auto-assigned if not set, disabled when door is open)

### Integration with Player Respawn System
The pressure plate system is integrated with the player respawn system. When the player respawns (press X), all pressure plates and doors will reset to their original positions.

### Example Usage:
1. Create a pressure plate GameObject with a trigger collider
2. Create a door GameObject with a SpriteRenderer and Collider2D
3. Assign the door to the pressure plate's "Door Object" field
4. The door will become invisible and passable when the player stands on the plate
5. The door will become visible and solid again when the player steps off the plate

### Tips:
- Use the visual feedback colors to easily see the plate's state
- Adjust the move speed for different gameplay feels
- Multiple pressure plates can control the same door
- The system works with the existing ghost replay system 