/* 
Wind: {Level.Wind}
HairColour: {Player.Hair.Color}
CameraPos: {Level.Camera.Position}
*/

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CelesteStudio.RichText;
using Microsoft.VisualBasic;
using StudioCommunication;
using WatsonWebsocket;

namespace CelesteStudio.Communication;

public static class VNyanWebSocket {
    
    private static string GetSetting(string param) {
        string result = "";
        string line;
        string setting;
        int pos;
        bool done = false;
        param = param.ToLower();
        //Pass the file path and file name to the StreamReader constructor
        StreamReader sr = new StreamReader(".\\CelesteTAV.config");
        //Read the first line of text
        line = sr.ReadLine();
        //Continue to read until you reach end of file
        while ((result == "") && (line != null)) {
            pos = line.IndexOf('=');
            setting = line.Substring(0, pos).Trim().ToLower();

            if (setting.Trim().ToLower() == param) {
                result = line.Substring(pos + 1);
            }
            line = sr.ReadLine();
        }
        //close the file
        sr.Close();
        if (result=="") {
            Console.WriteLine("Could not find setting: " + param);
        }
        Console.WriteLine("Read setting: "+param+" Result: " + result);
        return result;
    }

    private static int GetSettingInt(string param) {
        return Convert.ToInt32(GetSetting(param));
    }

    private static WatsonWsClient wsClient;
    private static bool wsConnected = false;
    private static System.Threading.CancellationToken CT = new System.Threading.CancellationToken();
    //private static string VNyanURL = Properties.Settings.Default.VNyanURL;
    private static string VNyanURL = GetSetting("VNyanURL");
    private static int DangerZoneX1 = GetSettingInt("VTuberDangerZoneX1");
    private static int DangerZoneX2 = GetSettingInt("VTuberDangerZoneX2");
    private static int DangerZoneY1 = GetSettingInt("VTuberDangerZoneY1");
    private static int DangerZoneY2 = GetSettingInt("VTuberDangerZoneY2");
    private static int SafeZoneX1 = GetSettingInt("VTuberSafeZoneX1");
    private static int SafeZoneX2 = GetSettingInt("VTuberSafeZoneX2");
    private static int SafeZoneY1 = GetSettingInt("VTuberSafeZoneY1");
    private static int SafeZoneY2 = GetSettingInt("VTuberSafeZoneY2");

    private static readonly string[] FirstSplit = { "\r\n", "\r", "\n" };
    private static readonly string[] SecondSplit = { ": " };
    private static readonly string[] ThirdSplit = { ", " };

    private static char oldWindDir = '0';
    private static string oldWindValue = "";
    private static string oldHairCol = "";
    private static bool oldDeadState = false;
    private static bool oldFeatherState = false;
    private static bool oldDashState = false;
    private static bool oldBadelineLaunchState = false;
    private static bool oldIntroJumpState = false;
    private static bool oldDangerZoneState = true;
    private static bool oldSwimState = false;
    private static bool oldBubbleState = false;
    private static bool oldRedDashState = false;
    private static string oldRoomName = "";
    private static int oldScreenXPos = 0;
    private static int oldScreenYPos = 0;
    private static bool firstConnect = true;
    private static string oldMenuState = "";

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
        int windSpeed = 0;
        // char windDir = '0';
        string windValue = "";
        string hairCol = "";
        bool deadState = false;
        bool featherState = false;
        bool dashState = false;
        bool badelineLaunchState = false;
        bool introJumpState = false;
        bool dangerZoneState = oldDangerZoneState;
        bool swimState = false;
        bool bubbleState = false;
        bool redDashState = false;
        string roomName = "";
        string menuState = "";
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

        menuState = "game";
        foreach (string line in lines) {
            if (line.Contains("Overworld")) { 
                menuState = line.Substring(10,line.Length-15);
            } else if (studioInfo.GameInfo.Contains("LevelLoader")) {
                menuState = "LevelLoader";
            } else if (studioInfo.GameInfo.Contains("LevelExit")) {
                menuState = "LevelExit";
            } else if (studioInfo.GameInfo.Contains("AreaComplete")) {
                menuState = "AreaComplete";
            } else if (line.Contains(": ")) {
                result = line.Split(SecondSplit, StringSplitOptions.None);
                if (result[0] == "Wind") {
                    temp = result[1].Split(ThirdSplit, StringSplitOptions.None);
                    windX = (int) (0-(Convert.ToDecimal(temp[0]))); // Because VNyan wind is model relative, not world relative
                    windY = (int) (0-(Convert.ToDecimal(temp[1])));
                    windSpeed = (int) Math.Round(Math.Sqrt((windX * windX) + (windY * windY)));
                    windValue = windX.ToString() + "," + windY.ToString() + "," + windSpeed.ToString();
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
                        hairCol = "#" + red.ToString("X2") + green.ToString("X2") + blue.ToString("X2") +"FF";
                        // hairCol = "{ \"Red\": \"" + red.ToString() + "\", \"Green\": \"" + green.ToString() + "\"; \"blue\": \"" + blue.ToString() + "\" }";
                        // hairCol = +red * 65536 + green * 256 + blue;

                    }
                } else if (result[0].Contains("Timer")) {
                    roomName = result[0].Substring(0, result[0].IndexOf("Timer")-1);
                } else if (result[0] == "Stamina") { // Status info is shown on the same line as stamina ( TODO: ST 20 = flashing)
                    string status = result[1].Substring(result[1].LastIndexOf(' ') + 1);
                    //MessageBox.Show("|"+status);
                    switch (status) { //   TODO: stDreamSash to add   DashCD on own line = white hair
                        case "StStarFly":
                            featherState = true;
                            break;
                        case "StSummitLaunch":
                            badelineLaunchState = true;
                            break;
                        case "StIntroJump":
                            introJumpState = true;
                            break;
                        case "StBoost":
                            bubbleState = true;
                            break;
                        case "StRedDash":
                            redDashState = true;
                            break;
                        case "StSwim":
                            swimState = true;
                            break;
                        case "StDash":
                            dashState = true;
                            swimState = oldSwimState;
                            break; // Can't detect dash and swim simultaneously, so assume we are remaining underwater
                    }              // If we dash out of water, SwimOff won't be sent until dash is finished.
                 }                 // Not much I can do about this, but it's only for a few ms so who cares!
            } else {
                if (line.Contains("Dead")) { deadState = true; }
            }
        }

        // Send state info to VNyan

        if (firstConnect) {
            RawSend("CelesteStart");
            firstConnect = false;
        }

        CompareAndSend(ref menuState, ref oldMenuState, "CelesteMenu");
        CompareAndSend(ref windValue, ref oldWindValue, "CelesteWind");
        CompareAndSend(ref deadState, ref oldDeadState, "CelesteDead");
        CompareAndSend(ref featherState, ref oldFeatherState, "CelesteFeather");
        CompareAndSend(ref dashState, ref oldDashState, "CelesteDash");
        CompareAndSend(ref badelineLaunchState, ref oldBadelineLaunchState, "CelesteBadelineLaunch");
        CompareAndSend(ref introJumpState, ref oldIntroJumpState, "CelesteIntroJump");
        CompareAndSend(ref hairCol, ref oldHairCol, "CelesteHairColour");
        CompareAndSend(ref dangerZoneState, ref oldDangerZoneState, "CelesteMadelineBehindVtuber");
        CompareAndSend(ref swimState, ref oldSwimState, "CelesteSwim");
        CompareAndSend(ref bubbleState, ref oldBubbleState, "CelesteBubble");
        CompareAndSend(ref redDashState, ref oldRedDashState, "CelesteRedBubbleDash");
        CompareAndSend(ref roomName, ref oldRoomName, "CelesteRoom");
    }
}

