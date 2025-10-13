# Waiting Room Feature

## Overview
The waiting room feature allows players to wait for opponents before starting a match. This ensures that games only start when exactly 2 players are present in the room.

## Features

### 1. Quick Match
- Players can click a "Quick Match" button to join a random available room
- If no room is available, a new room is automatically created
- Players are taken to the waiting room until an opponent joins

### 2. Custom Room
- Players can create a custom room with a specific code/name
- Other players can join using the same code
- Both players enter the waiting room until the second player joins

### 3. Waiting Room Mechanics
- **Player Count Display**: Shows current players vs required players (e.g., "Players: 1/2")
- **Status Messages**: 
  - "Waiting for another player..." - When waiting for opponent
  - "Player connected! Starting game..." - When second player joins
- **Automatic Game Start**: Game starts automatically 2 seconds after the second player joins
- **Player Disconnection Handling**: If a player leaves before the game starts, the countdown is cancelled

## Implementation Details

### Files Created/Modified

#### New Files:
1. **WaitingRoomManager.cs** - Main logic for waiting room
   - Monitors player count
   - Handles UI updates
   - Manages game start countdown
   - Handles player join/leave events

2. **sc_waiting_room.unity** - Waiting room scene
   - Scene where players wait before match starts

#### Modified Files:
1. **CreateAndJoinRooms.cs**
   - Added `QuickMatch()` method for random matchmaking
   - Added `OnJoinRandomFailed()` to create room if none available
   - Modified `OnJoinedRoom()` to redirect to waiting room instead of directly to game
   - Enhanced room options with proper visibility and max player settings

2. **EditorBuildSettings.asset**
   - Added sc_waiting_room scene to build settings

### Key Components

#### WaitingRoomManager
```csharp
public class WaitingRoomManager : MonoBehaviourPunCallbacks
```

**Public Properties:**
- `TextMeshProUGUI waitingText` - UI text for status messages
- `TextMeshProUGUI playerCountText` - UI text for player count
- `int requiredPlayers` - Number of players needed (default: 2)
- `float startGameDelay` - Delay before game start (default: 2 seconds)

**Public Methods:**
- `LeaveWaitingRoom()` - Allows player to leave waiting room and return to lobby

**Callback Methods:**
- `OnPlayerEnteredRoom()` - Updates UI when player joins
- `OnPlayerLeftRoom()` - Handles player leaving, cancels countdown if needed
- `OnLeftRoom()` - Returns to lobby when local player leaves

#### CreateAndJoinRooms
**Public Methods:**
- `CreateRoom()` - Creates a custom room with specified name
- `JoinRoom()` - Joins an existing custom room
- `QuickMatch()` - Joins random room or creates new one

## Usage

### For Quick Match:
1. Call `QuickMatch()` method on CreateAndJoinRooms component
2. Player automatically joins or creates a room
3. Player enters waiting room scene
4. Game starts when second player joins

### For Custom Room:
1. **Creating**: Call `CreateRoom()` with room name
2. **Joining**: Call `JoinRoom()` with room name
3. Both players enter waiting room
4. Game starts when both are present

### Scene Setup
To set up the waiting room scene in Unity:
1. Add WaitingRoomManager component to a GameObject
2. Assign UI Text elements:
   - `waitingText` - For status messages
   - `playerCountText` - For player count
3. Optionally add a "Leave" button that calls `LeaveWaitingRoom()`

## Room Management

### Room Properties:
- **MaxPlayers**: 2
- **IsVisible**: true (rooms appear in lobby list)
- **IsOpen**: true (rooms can be joined)

### Auto-Cleanup:
- Rooms are automatically managed by Photon
- When all players leave, room is destroyed
- No manual cleanup needed

## Testing

### Test Scenarios:
1. **Quick Match - First Player**: 
   - Should create room and show "Waiting for another player..."
   
2. **Quick Match - Second Player**:
   - Should join existing room
   - Both players should see "Player connected! Starting game..."
   - Game should start after 2 seconds

3. **Custom Room**:
   - Create room with code "TEST123"
   - Join from another client with same code
   - Should behave like Quick Match

4. **Player Disconnection**:
   - If player leaves before countdown finishes
   - Countdown should cancel
   - Remaining player should see waiting message again

5. **Leave Waiting Room**:
   - Player clicks leave button
   - Should return to lobby
   - Room should clean up if empty

## Configuration

### Adjustable Parameters in WaitingRoomManager:
- `requiredPlayers`: Change number of players needed (default: 2)
- `startGameDelay`: Adjust countdown time (default: 2.0 seconds)

## Error Handling

### Handled Scenarios:
- No UI elements assigned (null checks)
- Player leaves during countdown
- Room doesn't exist
- Connection issues (handled by Photon)

## Future Enhancements

Possible improvements:
- Countdown timer display
- Player ready status
- Chat during waiting
- Character selection in waiting room
- Tournament/team match support (more than 2 players)
