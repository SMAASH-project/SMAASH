# Quick Start Guide - Waiting Room Feature

## For Developers

### What Was Added

This PR adds a waiting room system for SMAASH multiplayer matches.

### How to Use

#### 1. Quick Match (Random Matchmaking)
In your UI, call the `QuickMatch()` method:

```csharp
// Get reference to CreateAndJoinRooms component
CreateAndJoinRooms roomManager = FindObjectOfType<CreateAndJoinRooms>();

// Start quick match
roomManager.QuickMatch();
```

Or via Unity Button OnClick event:
- Select your Quick Match button
- Add OnClick event
- Drag CreateAndJoinRooms component
- Select `QuickMatch()` method

#### 2. Custom Room Creation
```csharp
// Get reference and set room code
CreateAndJoinRooms roomManager = FindObjectOfType<CreateAndJoinRooms>();
roomManager.createInput.text = "MYROOM123";

// Create room
roomManager.CreateRoom();
```

#### 3. Custom Room Joining
```csharp
// Get reference and set room code
CreateAndJoinRooms roomManager = FindObjectOfType<CreateAndJoinRooms>();
roomManager.joinInput.text = "MYROOM123";

// Join room
roomManager.JoinRoom();
```

### Unity Editor Setup (Required!)

The waiting room scene needs UI setup:

1. **Open Scene**: Open `Assets/Scenes/sc_waiting_room.unity`

2. **Create UI Canvas** (if not exists):
   - Right-click in Hierarchy
   - UI → Canvas

3. **Create Status Text**:
   - Right-click Canvas → UI → Text - TextMeshPro
   - Name: "WaitingText"
   - Set text: "Waiting for another player..."
   - Position at top/center of screen
   - Increase font size (e.g., 36)

4. **Create Player Count Text**:
   - Right-click Canvas → UI → Text - TextMeshPro
   - Name: "PlayerCountText"
   - Set text: "Players: 1/2"
   - Position below WaitingText
   - Font size: 24

5. **Create WaitingRoomManager GameObject**:
   - Right-click in Hierarchy → Create Empty
   - Name: "WaitingRoomManager"
   - Add Component → WaitingRoomManager script

6. **Assign UI References**:
   - Select WaitingRoomManager GameObject
   - In Inspector, find WaitingRoomManager component
   - Drag "WaitingText" to `waitingText` field
   - Drag "PlayerCountText" to `playerCountText` field

7. **Optional - Add Leave Button**:
   - Right-click Canvas → UI → Button - TextMeshPro
   - Name: "LeaveButton"
   - Set text: "Leave"
   - Position at bottom of screen
   - In Button component OnClick:
     - Add entry
     - Drag WaitingRoomManager GameObject
     - Select `LeaveWaitingRoom()` method

8. **Save Scene**: Ctrl+S (Cmd+S on Mac)

### Testing in Unity Editor

#### Single Player Test (Create Room):
1. Play the game in Unity
2. Navigate to lobby
3. Create a room or click Quick Match
4. You should see:
   - "Waiting for another player..."
   - "Players: 1/2"

#### Two Player Test (Full Match):
1. Build the game (File → Build and Run)
2. Run one instance from build
3. Run one instance from Unity Editor (Play button)
4. Both click Quick Match
5. You should see:
   - Both players in waiting room
   - "Player connected! Starting game..."
   - "Players: 2/2"
   - Game starts after 2 seconds

### Customization

You can adjust parameters in the WaitingRoomManager component:

- **Required Players**: Change from 2 to any number (for team matches)
- **Start Game Delay**: Change from 2 seconds to desired delay

### Debugging

Check Unity Console for debug messages:
- "Quick Match initiated"
- "Player [name] entered the room. Total players: X"
- "Room already has X players - starting game immediately"
- "Starting game..."

### Common Issues

**Issue**: UI text not updating
- **Solution**: Make sure UI references are assigned in Inspector

**Issue**: Game not starting with 2 players
- **Solution**: Verify PhotonNetwork.AutomaticallySyncScene is enabled in PhotonServerSettings

**Issue**: "Error: Not in a room" message
- **Solution**: Make sure you're entering waiting room from CreateAndJoinRooms.OnJoinedRoom()

**Issue**: Multiple games starting simultaneously
- **Solution**: Verify only Master Client is loading level (handled in code)

### File Locations

- **Scripts**: `Assets/Scripts/Multiplayer/`
  - `WaitingRoomManager.cs` - Waiting room logic
  - `CreateAndJoinRooms.cs` - Room creation/joining (modified)
  
- **Scenes**: `Assets/Scenes/`
  - `sc_waiting_room.unity` - Waiting room scene
  - `sc_lobby.unity` - Where players start
  - `sc_main.unity` - Where game starts after waiting room

### Integration with Existing Code

The waiting room automatically integrates with:
- ✅ Photon PUN callbacks
- ✅ Scene management
- ✅ Room creation/joining
- ✅ Player synchronization

No additional configuration needed!

### Next Steps After Setup

1. Test Quick Match with 2 clients
2. Test Custom Room creation/joining
3. Test player disconnection during waiting
4. Customize UI appearance (colors, fonts, layout)
5. Add background music/effects to waiting room
6. Consider adding character preview or player info

### Support

For detailed documentation, see:
- `WAITING_ROOM_DOCUMENTATION.md` - Full feature documentation
- `IMPLEMENTATION_SUMMARY.md` - Technical implementation details

### Code Example - Complete Flow

```csharp
// Example: Quick Match Button Handler
public class LobbyUI : MonoBehaviour
{
    public CreateAndJoinRooms roomManager;
    
    public void OnQuickMatchButtonClick()
    {
        // This will:
        // 1. Try to join a random room
        // 2. If no room, create one
        // 3. Load waiting room scene
        // 4. Start game when 2 players present
        roomManager.QuickMatch();
    }
    
    public void OnCreateRoomButtonClick(string roomName)
    {
        roomManager.createInput.text = roomName;
        roomManager.CreateRoom();
    }
    
    public void OnJoinRoomButtonClick(string roomName)
    {
        roomManager.joinInput.text = roomName;
        roomManager.JoinRoom();
    }
}
```

That's it! The waiting room feature is ready to use. 🎮
