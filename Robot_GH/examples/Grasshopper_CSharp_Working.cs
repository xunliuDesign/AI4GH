// Grasshopper C# Script Component - Working Movement Format
// Based on documentation: T:110 for single joint control
// ⚠️ WARNING: This will move the arm! Use with caution!

#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using Rhino;
using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    #region Notes
    /* 
      Members:
        RhinoDoc RhinoDocument
        GH_Document GrasshopperDocument
        IGH_Component Component
        int Iteration
      Methods (Virtual & overridable):
        Print(string text)
        Print(string format, params object[] args)
        Reflect(object obj)
        Reflect(object obj, string method_name)
    */
    #endregion
    
    private void RunScript(List<Point3d> x, string IP, bool Execute, int Joint, double Angle, int Speed, ref object a, ref object b)
    {
        // Format from documentation: {"T": 110, "joint": 1, "angle": 45, "spd": 10}
        // Angle is in DEGREES (not radians!)
        //
        // INPUTS:
        //   x - List<Point3d> - Points (for future IK)
        //   IP - string - IP address
        //   Execute - bool - Execute command
        //   Joint - int - Joint number (1=base, 2=shoulder, 3=elbow, 4=hand/wrist)
        //   Angle - double - Angle in DEGREES (0 = no change, or absolute position)
        //   Speed - int - Speed coefficient (1-100, lower = slower)
        //
        // OUTPUTS:
        //   a - object - Results
        //   b - string - Status
        
        string ipAddress = string.IsNullOrEmpty(IP) ? "192.168.4.1" : IP;
        string status = "";
        object result = null;
        
        // Safety limits in DEGREES
        var safeLimits = new Dictionary<int, Tuple<double, double>>
        {
            { 1, Tuple.Create(-90.0, 90.0) },   // Base
            { 2, Tuple.Create(-45.0, 90.0) },    // Shoulder (prevent table collision)
            { 3, Tuple.Create(-90.0, 90.0) },    // Elbow
            { 4, Tuple.Create(-90.0, 90.0) }     // Hand/wrist
        };
        
        // Validate inputs
        if (Joint < 1 || Joint > 4)
        {
            status = $"❌ Invalid joint number: {Joint} (must be 1-4)";
            Print(status);
            a = null;
            b = status;
            return;
        }
        
        if (Speed < 1) Speed = 1;
        if (Speed > 100) Speed = 100;
        
        if (Execute)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    
                    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    string json;
                    string url;
                    
                    // If Angle is 0, send status query (safe, won't move)
                    if (Angle == 0)
                    {
                        var statusCmd = new { T = timestamp };
                        json = JsonSerializer.Serialize(statusCmd);
                        url = $"http://{ipAddress}/js?json={Uri.EscapeDataString(json)}";
                        Print($"Status query: {json}");
                    }
                    else
                    {
                        // CORRECT FORMAT: CMD_SINGLE_JOINT_ANGLE (T:121)
                        // Official format: {"T":121,"joint":1,"angle":0,"spd":10,"acc":10}
                        // joint: 0=base, 1=shoulder, 2=elbow, 3=hand
                        // angle: in DEGREES
                        // spd: speed (0-100, 0 = max speed)
                        // acc: acceleration (0-254, 10 = 1000 steps/s²)
                        
                        // Safety check
                        if (safeLimits.ContainsKey(Joint))
                        {
                            var limits = safeLimits[Joint];
                            if (Angle < limits.Item1 || Angle > limits.Item2)
                            {
                                status = $"❌ SAFETY BLOCKED: Angle {Angle}° outside safe range [{limits.Item1}, {limits.Item2}]°";
                                Print(status);
                                a = null;
                                b = status;
                                return;
                            }
                        }
                        
                        if (Speed > 10)
                        {
                            Print($"⚠️ WARNING: High speed ({Speed}) - arm may move fast!");
                        }
                        
                        // Convert angle from degrees to match input
                        // Official format uses joint 0-3 (0=base, 1=shoulder, 2=elbow, 3=hand)
                        var command = new
                        {
                            T = 121,  // CMD_SINGLE_JOINT_ANGLE
                            joint = Joint - 1,  // 0=base, 1=shoulder, 2=elbow, 3=hand
                            angle = Angle,  // In DEGREES
                            spd = Speed,    // Speed (0-100, 0 = max)
                            acc = 10        // Acceleration (0-254, 10 = 1000 steps/s²)
                        };
                        
                        json = JsonSerializer.Serialize(command);
                        url = $"http://{ipAddress}/js?json={Uri.EscapeDataString(json)}";
                        Print($"Movement command: {json}");
                        Print($"⚠️ Moving Joint {Joint} to {Angle}° at speed {Speed}");
                    }
                    
                    // Send request
                    Print($"Sending to: {url.Substring(0, Math.Min(120, url.Length))}...");
                    var response = client.GetAsync(url).Result;
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var readTask = response.Content.ReadAsStringAsync();
                        string responseText = readTask.Result;
                        
                        status = $"✓ Command sent: Joint {Joint}, Angle {Angle}, Speed {Speed}";
                        if (!string.IsNullOrEmpty(responseText))
                        {
                            status += $"\n   Response: {responseText.Substring(0, Math.Min(100, responseText.Length))}";
                        }
                        result = responseText;
                    }
                    else
                    {
                        status = $"✗ Error: HTTP {response.StatusCode}";
                        result = null;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                status = $"⚠ Timeout: Command took longer than 30s";
                status += "\n   This might mean:";
                status += "\n   1. Arm is still processing (check if it moved!)";
                status += "\n   2. Command format might be wrong";
                status += "\n   3. Try a status query first: Set Angle=0";
                Print(status);
                Print("   💡 TIP: Check if the arm moved - timeout doesn't always mean failure!");
                result = null;
            }
            catch (AggregateException aggEx)
            {
                // Unwrap AggregateException to get the real error
                Exception realEx = aggEx.InnerException ?? aggEx;
                status = $"✗ Error: {realEx.GetType().Name}: {realEx.Message}";
                
                // Check for specific network errors
                if (realEx is System.Net.Http.HttpRequestException httpEx)
                {
                    status += "\n   Network Error - Check:";
                    status += "\n   1. Connected to RoArm WiFi (192.168.4.1)";
                    status += "\n   2. Robot is powered on";
                    status += "\n   3. Can you access http://192.168.4.1 in browser?";
                }
                else if (realEx is System.Net.Sockets.SocketException)
                {
                    status += "\n   Socket Error - Cannot connect to robot";
                }
                else if (realEx is System.Net.WebException)
                {
                    status += "\n   Web Exception - Network issue";
                }
                
                Print(status);
                Print($"   Full error: {realEx}");
                result = null;
            }
            catch (Exception ex)
            {
                status = $"✗ Error: {ex.GetType().Name}: {ex.Message}";
                if (ex.InnerException != null)
                {
                    status += $"\n   Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
                }
                Print(status);
                Print($"   Stack: {ex.StackTrace}");
                result = null;
            }
        }
        else
        {
            status = "⏸ Execute is False";
        }
        
        a = result;
        b = status;
    }
}

