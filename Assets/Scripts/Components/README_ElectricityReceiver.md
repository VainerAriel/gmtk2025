# Electricity Receiver System

The Electricity Receiver is a component that detects electricity projectiles and activates connected doors, similar to the Pressure Plate system but triggered by electricity instead of pressure.

## Setup Instructions

### 1. Create an Electricity Receiver

1. Drag the `ElectricityReceiver.prefab` from the Prefabs folder into your scene
2. Position it where you want the receiver to be
3. The receiver will automatically set up its trigger collider

### 2. Connect to a Door

1. Select the Electricity Receiver in the scene
2. In the Inspector, find the "Door Object" field in the "Receiver Settings" section
3. Drag your door GameObject into this field
4. The door will automatically get a DoorController component if it doesn't have one

### 3. Configure Settings

- **Move Direction**: The direction the door will move when activated (default: right)
- **Move Distance**: How far the door moves in grid units (default: 1)
- **Move Speed**: Speed of door movement (default: 2)
- **Activated Color**: Color when receiver is activated (default: yellow)
- **Deactivated Color**: Color when receiver is inactive (default: gray)

### 4. Visual Setup

1. Select the Electricity Receiver
2. In the Inspector, find the "Receiver Renderer" field in the "Visual Feedback" section
3. Drag the SpriteRenderer component from the receiver into this field
4. The receiver will change color when activated/deactivated

## How It Works

1. **Detection**: The receiver has a trigger collider that detects electricity projectiles
2. **Activation**: When hit by an electricity projectile, the receiver activates
3. **Door Control**: The connected door opens when the receiver is activated
4. **Visual Feedback**: The receiver changes color to indicate its state
5. **Debouncing**: Built-in cooldown prevents rapid toggling

## Testing

1. Place an electricity emitter (ProjectileShooter) in your scene
2. Configure the emitter to shoot electricity projectiles
3. Aim the emitter at the electricity receiver
4. When the electricity hits the receiver, the connected door should open

## Script Reference

### Public Methods

- `IsActivated()`: Returns true if the receiver is currently activated
- `ResetReceiver()`: Resets the receiver to its initial state
- `ManualActivate()`: Manually activates the receiver (for testing)
- `ManualDeactivate()`: Manually deactivates the receiver (for testing)

### Events

The receiver automatically handles:
- `OnTriggerEnter2D`: Detects electricity projectiles via trigger
- `OnCollisionEnter2D`: Detects electricity projectiles via collision

## Integration with Existing Systems

The Electricity Receiver integrates seamlessly with:
- **DoorController**: Uses the same door system as Pressure Plates
- **Projectile System**: Detects the same electricity projectiles used by other systems
- **Layer System**: Uses the default layer for proper collision detection

## Troubleshooting

1. **Receiver not detecting electricity**: Check that the electricity projectile has a Projectile component
2. **Door not opening**: Ensure the door has a DoorController component
3. **No visual feedback**: Make sure the Receiver Renderer is assigned in the inspector
4. **Rapid toggling**: The system has a built-in cooldown to prevent this

## Example Setup

```
Scene Hierarchy:
├── ElectricityReceiver (ElectricityReceiver.prefab)
│   ├── Transform
│   ├── SpriteRenderer
│   ├── Rigidbody2D
│   ├── BoxCollider2D (Trigger)
│   └── ElectricityReceiver (Script)
└── Door (Your door GameObject)
    ├── Transform
    ├── SpriteRenderer
    ├── Collider2D
    └── DoorController (Auto-added)
```

The Electricity Receiver provides a simple way to create electricity-based door controls that integrate perfectly with your existing door and projectile systems. 