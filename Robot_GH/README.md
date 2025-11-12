# RoArm-M2-S Grasshopper Integration

✅ **WORKING** - Successfully tested and functional!

## Quick Start

### 1. Connect to Robot WiFi
- Connect your computer to the robot's WiFi network
- Default IP: `192.168.4.1` (AP mode)
- You won't have internet while connected (this is normal)

### 2. Use in Grasshopper

1. **Add C# Script component** in Grasshopper
2. **Copy code** from `examples/Grasshopper_CSharp_Working.cs`
3. **Add inputs**:
   - `x` (Point3d) - Points (for future IK implementation)
   - `IP` (string) - Robot IP address (default: "192.168.4.1")
   - `Execute` (bool) - Execute command
   - `Joint` (int) - Joint number: **1=base, 2=shoulder, 3=elbow, 4=hand**
   - `Angle` (double) - Angle in **DEGREES** (e.g., 10, 45, 90)
   - `Speed` (int) - Speed (1-100, 0 = max speed, higher = slower)
4. **Add outputs**:
   - `a` (object) - Response from robot
   - `b` (string) - Status message
5. **Test with small angles first!**

### 3. Example Usage

- **Status query** (safe, won't move): Set `Angle = 0`
- **Move shoulder**: `Joint = 2`, `Angle = 10`, `Speed = 10`
- **Move base**: `Joint = 1`, `Angle = 45`, `Speed = 5`

## Command Format

The component uses the **official Waveshare format**:

```json
{"T":121,"joint":1,"angle":10,"spd":10,"acc":10}
```

- **T: 121** - `CMD_SINGLE_JOINT_ANGLE` command
- **joint**: 0=base, 1=shoulder, 2=elbow, 3=hand
- **angle**: Angle in **DEGREES**
- **spd**: Speed (0-100, 0 = max speed)
- **acc**: Acceleration (0-254, default: 10)

## API Endpoint

- **URL**: `http://{ip}/js?json={command}`
- **Method**: GET
- **Format**: JSON command in URL-encoded query parameter

## Safety Features

- ✅ Safety limits for joint angles (prevents table collisions)
- ✅ Speed warnings for high speeds
- ✅ Status query mode (Angle=0) for safe testing
- ✅ Input validation

## Files

- `examples/Grasshopper_CSharp_Working.cs` - **Main working component** ✅
- `roarm_controller.py` - Python controller (for reference/testing)

## Troubleshooting

### Robot not responding
1. Check WiFi connection (must be on robot's network)
2. Restart robot (power cycle)
3. Verify IP address (default: 192.168.4.1)
4. Test in browser: `http://192.168.4.1`

### Arm not moving
1. Check `Execute = True`
2. Verify `Angle` is not 0 (0 = status query only)
3. Check `Speed` is reasonable (1-100)
4. Try small angles first (5-10°)

### Timeout errors
- Movement commands may take time to process
- Check if arm actually moved (timeout doesn't always mean failure)
- Increase timeout if needed (currently 10 seconds)

## Official Documentation

- [Waveshare Wiki - HTTP Communication](https://www.waveshare.com/wiki/RoArm-M2-S_Python_HTTP_Request_Communication)
- [Waveshare Wiki - JSON Commands](https://www.waveshare.com/wiki/RoArm-M2-S_JSON_Command_Meaning)

## Notes

- Works in **Rhino 7 and 8** (Mac/Windows)
- Uses built-in `System.Text.Json` (no NuGet packages needed)
- Status queries (`Angle=0`) are safe and won't move the arm
- Movement commands will move the arm - use caution!

---

**Last Updated**: Working version with correct `CMD_SINGLE_JOINT_ANGLE` (T:121) format

