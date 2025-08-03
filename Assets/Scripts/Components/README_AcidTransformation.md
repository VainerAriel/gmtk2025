# Acid Transformation System

## Overview
The acid transformation system adds a new ghost transformation type that triggers when the player dies from acid damage. This transformation creates an "acid explosion" effect that destroys adjacent breakable blocks.

## Components

### 1. BreakableBlock (`BreakableBlock.cs`)
A new block type that behaves like ground but can be destroyed by ghost transformations.

**Features:**
- Set to Ground layer (layer 8) for proper collision
- Configurable visual appearance (normal and damaged colors)
- Destruction effects and sounds
- Grid-snapped positioning
- Debug mode for testing

**Usage:**
- Place `BreakableBlock.prefab` in your scene
- Configure colors, effects, and debug settings in the inspector
- Blocks will be destroyed when a ghost transforms from acid death

### 2. Modified PlayerController
Added acid death tracking to the existing player system.

**New Features:**
- `diedFromAcid` flag to track acid deaths
- `DiedFromAcid()` method to check acid death state
- `ResetAcidDeathState()` method to clear acid death state
- Modified `TakeDamage()` to detect acid deaths
- Updated ghost creation to pass acid death information

### 3. Modified GhostController
Added acid transformation support to the existing ghost system.

**New Features:**
- `hasBeenHitByAcid` flag for acid transformation state
- `TransformIntoAcidExplosion()` method for acid transformation
- `ResetAcidHitState()` method to reset acid transformation state
- Modified `Initialize()` to accept acid death parameter
- Automatic acid transformation when ghost is created from acid death

## Acid Transformation Behavior

### Trigger Condition
- Player dies while poisoned (from acid pools or acid projectiles)
- Ghost is created with acid death information

### Transformation Effect
The ghost disappears and destroys breakable blocks in 6 adjacent positions:
- 2 tiles to the left
- 2 tiles to the right  
- 1 tile up
- 1 tile down

### Grid Snapping
All positions are snapped to the 1-unit grid for precise block detection.

## Testing

### AcidTransformationTest Script
A test script for demonstrating the acid transformation system.

**Controls:**
- Press `B` to create a grid of breakable blocks
- Press `A` to manually trigger an acid death
- Use `ClearAllBreakableBlocks()` to remove all breakable blocks

**Setup:**
1. Add `AcidTransformationTest` component to a GameObject in your scene
2. Assign the `BreakableBlock.prefab` to the test script
3. Set a test area transform (optional)
4. Enable debug mode for detailed logging

## Integration with Existing Systems

### Ghost System
- Acid transformation integrates seamlessly with existing electricity and spike transformations
- Ghosts can only transform once per death (electricity, spike, or acid)
- All transformation states are reset when the game state resets

### Acid System
- Uses existing acid pool and projectile damage system
- Leverages existing poison status tracking
- No changes required to existing acid components

### Death System
- Integrates with existing instant death detection
- Maintains compatibility with electricity and spike deaths
- Preserves existing respawn mechanics

## Usage in Game Design

### Strategic Placement
- Place breakable blocks strategically to create paths or remove obstacles
- Use acid deaths to clear specific areas for subsequent attempts
- Combine with other transformation types for complex puzzle solutions

### Level Design
- Create areas that require acid deaths to progress
- Design puzzles that use the 6-tile destruction pattern
- Balance difficulty with the limited number of ghosts

### Visual Feedback
- Breakable blocks can show warning colors before destruction
- Destruction effects and sounds provide clear feedback
- Debug logging helps with testing and development

## Technical Details

### Performance
- Grid snapping ensures efficient block detection
- OverlapPointAll used for precise collision detection
- Minimal memory footprint with simple state tracking

### Debugging
- Comprehensive debug logging throughout the system
- Test script for easy validation
- Inspector-exposed settings for tuning

### Extensibility
- Easy to modify destruction pattern (currently 6 adjacent tiles)
- Configurable visual and audio effects
- Modular design allows for additional transformation types 