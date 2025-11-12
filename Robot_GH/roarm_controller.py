"""
RoArm-M2-S Controller for Grasshopper
This script can be used in a Grasshopper Python component to control the robotic arm
"""

import urllib.request
import urllib.parse
import json
import math

class RoArmController:
    """Controller class for RoArm-M2-S robotic arm via HTTP/JSON"""
    
    def __init__(self, ip_address="192.168.4.1", port=80):
        """
        Initialize the controller
        
        Args:
            ip_address: IP address of the RoArm-M2-S (default: 192.168.4.1)
            port: Port number (default: 80)
        """
        self.base_url = f"http://{ip_address}:{port}"
        # RoArm-M2-S uses GET request to /js?json={...} format
        self.api_endpoint = f"{self.base_url}/js"
        import time
        self.base_timestamp = int(time.time() * 1000)  # Milliseconds timestamp
        
    def send_command(self, command_dict):
        """
        Send a JSON command to the robotic arm
        Uses GET request with JSON in query string: /js?json={...}
        
        Args:
            command_dict: Dictionary containing the command (will be converted to JSON)
            
        Returns:
            Response from the arm (if any)
        """
        try:
            # Convert command to JSON string
            json_str = json.dumps(command_dict, separators=(',', ':'))
            # URL encode the JSON
            encoded_json = urllib.parse.quote(json_str)
            # Build full URL
            url = f"{self.api_endpoint}?json={encoded_json}"
            
            # Create GET request
            req = urllib.request.Request(url)
            
            # Send request
            with urllib.request.urlopen(req, timeout=5) as response:
                return response.read().decode('utf-8')
                
        except Exception as e:
            print(f"Error sending command: {e}")
            return None
    
    def send_raw_command(self, T=None, m=None, axis=None, cmd=None, spd=None):
        """
        Send raw command in RoArm-M2-S format
        
        Args:
            T: Timestamp (auto-generated if None, or can be just timestamp for status query)
            m: Mode (optional)
            axis: Axis/joint number (0-4, optional)
            cmd: Command value (optional)
            spd: Speed (0-100, optional)
        """
        import time
        if T is None:
            T = int(time.time() * 1000)  # Current timestamp in milliseconds
        
        command = {"T": T}
        
        if m is not None:
            command["m"] = m
        if axis is not None:
            command["axis"] = axis
        if cmd is not None:
            command["cmd"] = cmd
        if spd is not None:
            command["spd"] = spd
            
        return self.send_command(command)
    
    def get_status(self):
        """
        Get status from the arm (just sends timestamp)
        Returns current status/state of the arm
        """
        import time
        T = int(time.time() * 1000)
        return self.send_command({"T": T})
    
    def set_joint_angle(self, axis, angle, speed=10):
        """
        Set a single joint angle (in degrees)
        
        Args:
            axis: Joint number (0=base, 1=shoulder, 2=elbow, 3=wrist, 4=gripper)
            angle: Angle in degrees
            speed: Movement speed (0-100, default: 10)
        """
        # Command format: cmd=1 means set angle, angle value goes in 'T' or separate field
        # Based on example, we need to map angle to the command structure
        # This may need adjustment based on actual API documentation
        return self.send_raw_command(m=1, axis=int(axis), cmd=int(angle), spd=speed)
    
    def set_joint_angles(self, joint1, joint2, joint3, joint4, speed=10):
        """
        Set all joint angles (in degrees)
        
        Args:
            joint1: Base rotation angle (degrees)
            joint2: Shoulder angle (degrees)
            joint3: Elbow angle (degrees)
            joint4: Wrist angle (degrees)
            speed: Movement speed (0-100, default: 10)
        """
        results = []
        # Set each joint sequentially
        results.append(self.set_joint_angle(0, joint1, speed))
        results.append(self.set_joint_angle(1, joint2, speed))
        results.append(self.set_joint_angle(2, joint3, speed))
        results.append(self.set_joint_angle(3, joint4, speed))
        return results
    
    def move_to_position(self, x, y, z, speed=10):
        """
        Move end effector to a specific position (in mm)
        NOTE: This may require inverse kinematics calculation
        For now, this is a placeholder - you may need to calculate joint angles first
        
        Args:
            x: X coordinate in mm
            y: Y coordinate in mm
            z: Z coordinate in mm
            speed: Movement speed (0-100, default: 10)
        """
        # TODO: Implement inverse kinematics or use position command if available
        # For now, this needs the actual command format for position control
        # You may need to use set_joint_angles with calculated angles instead
        print("Warning: move_to_position requires inverse kinematics or position command format")
        print("Consider using set_joint_angles with calculated angles instead")
        return None
    
    def control_gripper(self, value, speed=10):
        """
        Control the gripper
        
        Args:
            value: Gripper value (0-100, where 0=closed, 100=open)
            speed: Movement speed (0-100, default: 10)
        """
        # Axis 4 is typically the gripper
        return self.send_raw_command(m=1, axis=4, cmd=int(value), spd=speed)
    
    def open_gripper(self, speed=10):
        """Open the gripper"""
        return self.control_gripper(100, speed)
    
    def close_gripper(self, speed=10):
        """Close the gripper"""
        return self.control_gripper(0, speed)
    
    def follow_path(self, points, speed=10):
        """
        Follow a path defined by a list of points
        NOTE: This requires inverse kinematics to convert (x,y,z) to joint angles
        For now, you should use set_joint_angles with pre-calculated angles
        
        Args:
            points: List of (x, y, z) tuples or list of Point3d objects from Rhino
            speed: Movement speed (0-100)
        """
        print("Warning: follow_path requires inverse kinematics")
        print("For now, use set_joint_angles with calculated angles")
        print("Or implement IK solver to convert (x,y,z) to joint angles")
        return []


# Example usage in Grasshopper Python component:
# 
# import roarm_controller
# 
# # Initialize controller (set IP address of your RoArm-M2-S)
# arm = roarm_controller.RoArmController(ip_address="192.168.4.1")
# 
# # Get points from Grasshopper input
# points = x  # Assuming 'x' is the input from Grasshopper containing path points
# 
# # Follow the path
# results = arm.follow_path(points)
# 
# # Output results
# a = results

