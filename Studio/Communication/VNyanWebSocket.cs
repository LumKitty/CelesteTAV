/* 
Wind: {Level.Wind}
HairColour: {Player.Hair.Color}
CameraPos: {Level.Camera.Position}
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using StudioCommunication;
using WatsonWebsocket;

namespace CelesteStudio.Communication;
public static class VNyanWebSocket {
    
    private static WatsonWsClient wsClient;
    private static bool wsConnected = false;
    private static System.Threading.CancellationToken CT = new System.Threading.CancellationToken();
    private static readonly string VNyanURL = Properties.Settings.Default.VNyanURL;
    private static readonly int DangerZoneX1 = Properties.Settings.Default.VTuberDangerZoneX1;
    private static readonly int DangerZoneX2 = Properties.Settings.Default.VTuberDangerZoneX2;
    private static readonly int DangerZoneY1 = Properties.Settings.Default.VTuberDangerZoneY1;
    private static readonly int DangerZoneY2 = Properties.Settings.Default.VTuberDangerZoneY2;
    private static readonly int SafeZoneX1 = Properties.Settings.Default.VTuberSafeZoneX1;
    private static readonly int SafeZoneX2 = Properties.Settings.Default.VTuberSafeZoneX2;
    private static readonly int SafeZoneY1 = Properties.Settings.Default.VTuberSafeZoneY1;
    private static readonly int SafeZoneY2 = Properties.Settings.Default.VTuberSafeZoneY2;

    private static readonly string[] FirstSplit = { "\r\n", "\r", "\n" };
    private static readonly string[] SecondSplit = { ": " };
    private static readonly string[] ThirdSplit = { ", " };

    private static char oldWindDir = '0';
    private static int oldWindValue = 0;
    private static int oldHairCol = 0;
    private static bool oldDeadState = false;
    private static bool oldFeatherState = false;
    private static bool oldDashState = false;
    private static bool oldBadelineLaunchState = false;
    private static bool oldIntroJumpState = false;
    private static bool oldDangerZoneState = true;
    private static bool oldSwimState = false;
    private static int oldScreenXPos = 0;
    private static int oldScreenYPos = 0;

    private static void Log (string message) {
        Console.WriteLine(message);
    }

    private static void WebSocketConnected(object sender, EventArgs args) {
        wsConnected = true;
        Log("Connected to "+VNyanURL);
    }
    private static void WebSocketDisonnected(object sender, EventArgs args) {
        wsConnected = false;
        Log("Disonnected from "+VNyanURL);
    }
    
    private static void CompareAndSend(ref string newValue, ref string oldValue, string message) {
        if (newValue != oldValue) {
            message += " " + newValue;
            wsClient.SendAsync(message, WebSocketMessageType.Text, CT);
            Log("String: " + message);
            if (wsConnected) { oldValue = newValue; }
        }
    }
    private static void CompareAndSend(ref int newValue, ref int oldValue, string message) {
        // Console.WriteLine(newValue.ToString() + " " + oldValue.ToString());
        if (newValue != oldValue) {
            message += " " + newValue.ToString();
            wsClient.SendAsync(message, WebSocketMessageType.Text, CT);
            Log("Int   : " + message);
            if (wsConnected) { oldValue = newValue; }
        }
    }

    private static void RawSend(string message) {
        wsClient.SendAsync(message, WebSocketMessageType.Text, CT);
        Log("Raw   : "+message);
    }

    private static void CompareAndSend(ref bool newValue, ref bool oldValue, string message) {
        if (newValue != oldValue) {
            if (newValue) { message += " 1"; } else { message += " 0"; }
            wsClient.SendAsync(message, WebSocketMessageType.Text, CT);
            Log("Bool  : "+ message);
            if (wsConnected) { oldValue = newValue; }
        }
    }

    public static void DoVNyanComms(StudioInfo studioInfo) {
        string[] temp;
        string[] result;
        int windX = 0;
        int windY = 0;
        char windDir = '0';
        int windValue = 0;
        int hairCol = 0;
        bool deadState = false;
        bool featherState = false;
        bool dashState = false;
        bool badelineLaunchState = false;
        bool introJumpState = false;
        bool dangerZoneState = oldDangerZoneState;
        bool swimState = false;
        int screenXPos = -1;
        int screenYPos = -1;
        int globalXPos = -1;
        int globalYPos = -1;
        int cameraXPos = -1;
        int cameraYPos = -1;

        //int windSpeed = 0;
        //string windCategory = "";
        string[] lines = studioInfo.GameInfo.Split(FirstSplit, StringSplitOptions.None);
        if (!wsConnected) {
            wsClient = new WatsonWsClient(new Uri(VNyanURL));
            wsClient.ServerConnected += WebSocketConnected;
            wsClient.ServerDisconnected += WebSocketDisonnected;
            wsClient.KeepAliveInterval = 1000;
            wsClient.Start();
        }
        foreach (string line in lines) {
            if (line.Contains(": ")) {
                result = line.Split(SecondSplit, StringSplitOptions.None);
                if (result[0] == "Wind") {
                    temp = result[1].Split(ThirdSplit, StringSplitOptions.None);
                    windX = (int) Convert.ToDecimal(temp[0]) / 100;
                    windY = (int) Convert.ToDecimal(temp[1]) / 100;
                    if (windX == 0) {
                        if (windY == 0) {
                            windDir = '0';
                        } else {
                            if (windY < 0) {
                                windDir = 'U';
                            } else {
                                windDir = 'D';
                            }
                            windValue = Math.Abs(windY);
                        }
                    } else {
                        if (windX < 0) {
                            windDir = 'L';
                        } else {
                            windDir = 'R';
                        }
                        windValue = Math.Abs(windX);
                    }
                } else if (result[0] == "Pos") {
                    temp = result[1].Split(ThirdSplit, StringSplitOptions.None);
                    globalXPos = (int) Convert.ToDecimal(temp[0]);
                    globalYPos = (int) Convert.ToDecimal(temp[1]);
                } else if (result[0] == "CameraPos") {
                    temp = result[1].Split(ThirdSplit, StringSplitOptions.None);
                    cameraXPos = (int) Convert.ToDecimal(temp[0]);
                    cameraYPos = (int) Convert.ToDecimal(temp[1]);
                    screenXPos = globalXPos - cameraXPos;
                    screenYPos = globalYPos - cameraYPos;
                    if (oldDangerZoneState) {
                        if (screenXPos < SafeZoneX1 || screenXPos > SafeZoneX2 || screenYPos < SafeZoneY1 || screenYPos > SafeZoneY2) {
                            //MessageBox.Show(screenXPos.ToString() + "|"+SafeZoneX2.ToString());
                            dangerZoneState = false;
                        }
                    } else {
                        if (screenXPos > DangerZoneX1 && screenXPos < DangerZoneX2 && screenYPos > DangerZoneY1 && screenYPos < DangerZoneY2) {
                            dangerZoneState = true;
                        }
                    }

                    //Console.Write("X: " + screenXPos.ToString() + " Y: " + screenYPos.ToString() + "|" + DangerZoneX1.ToString() + " " + DangerZoneX2.ToString() + " " + DangerZoneY1.ToString() + " " + DangerZoneY2.ToString() +"\n");
                } else if (result[0] == "HairColour") {
                    if (result[1].Contains("}")) {
                        string colStr = result[1].Substring(result[1].IndexOf('{') + 1);
                        colStr = colStr.Substring(0, colStr.IndexOf("A") - 1);
                        string[] RGBStr = colStr.Split(' ');
                        byte red = Convert.ToByte(RGBStr[0].Substring(2));
                        byte green = Convert.ToByte(RGBStr[1].Substring(2));
                        byte blue = Convert.ToByte(RGBStr[2].Substring(2));
                        hairCol = +red*65536 + green * 256 + blue;
                    }
                } else if (result[0] == "Stamina") { // Status info is shown on the same line as stamina
                    string status = result[1].Substring(result[1].LastIndexOf(' ')+1);
                    //MessageBox.Show("|"+status);
                    switch (status) {
                        case "StStarFly":      featherState        = true; break;
                        case "StSummitLaunch": badelineLaunchState = true; break;
                        case "StIntroJump":    introJumpState      = true; break;
                        case "StSwim":         swimState           = true; break;
                        case "StDash":         dashState = true; swimState = oldSwimState; break; // Can't detect dash and swim simultaneously, so assume we are remaining underwater
                    }                                                                             // If we dash out of water, SwimOff won't be sent until dash is finished.
                }                                                                                 // Not much I can do about this, but it's only for a few ms so who cares!
            } else {
                if (line.Contains("Dead")) { deadState = true; }
            }
        }
        if (!swimState && oldSwimState || (introJumpState && !oldIntroJumpState)) { // My swiming animation messes with wind, so force a reset to happen
            oldWindDir = '-';
            oldWindValue = 0;
        }
        if (oldWindDir != windDir) {
            RawSend("CelesteWind" + windDir + ' ' + windValue.ToString());
            if (wsConnected) { 
                oldWindDir = windDir;
                oldWindValue = windValue;
            }
        } else {
            CompareAndSend(ref windValue,       ref oldWindValue,           "CelesteWind" + windDir);
        }
        CompareAndSend(ref deadState,           ref oldDeadState,           "CelesteDead");
        CompareAndSend(ref featherState,        ref oldFeatherState,        "CelesteFeather");
        CompareAndSend(ref dashState,           ref oldDashState,           "CelesteDash");
        CompareAndSend(ref badelineLaunchState, ref oldBadelineLaunchState, "CelesteBadelineLaunch");
        CompareAndSend(ref introJumpState,      ref oldIntroJumpState,      "CelesteIntroJump");
        CompareAndSend(ref hairCol,             ref oldHairCol,             "CelesteHairColour");
        CompareAndSend(ref dangerZoneState,     ref oldDangerZoneState,     "CelesteMadelineBehindVtuber");
        CompareAndSend(ref swimState,           ref oldSwimState,           "CelesteSwim");
    }
}

