using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using KSP.UI.Screens;
using USITools;
using System.Collections.Generic;

namespace Konstruction
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ServoMonitor : MonoBehaviour
    {
        private ApplicationLauncherButton servoButton;
        private IButton planLogTButton;
        private Rect _windowPosition = new Rect(300, 60, 700, 400);
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _scrollStyle;
        private Vector2 scrollPos = Vector2.zero;
        private bool _hasInitStyles = false;
        private bool windowVisible;
        public static bool renderDisplay = false;
        private List<bool> showServo;


        void Awake()
        {
            var texture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
            var textureFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Servo.png");
            print("Loading " + textureFile);
            texture.LoadImage(File.ReadAllBytes(textureFile));
            this.servoButton = ApplicationLauncher.Instance.AddModApplication(GuiOn, GuiOff, null, null, null, null,
                ApplicationLauncher.AppScenes.ALWAYS, texture);
        }

        private void GuiOn()
        {
            renderDisplay = true;
        }

        public void Start()
        {
            if (!_hasInitStyles)
                InitStyles();
            showServo = new List<bool>();
        }

        private void GuiOff()
        {
            renderDisplay = false;
        }

        private void OnGUI()
        {
            try
            {
                if (!renderDisplay)
                    return;

                if (Event.current.type == EventType.Repaint || Event.current.isMouse)
                {
                    //preDrawQueue
                }
                Ondraw();
            }
            catch (Exception ex)
            {
                print("ERROR in ServoMonitor (OnGui) " + ex.Message);
            }
        }

        private void Ondraw()
        {
            _windowPosition = GUILayout.Window(10, _windowPosition, OnWindow, "Servo Controller", _windowStyle);
        }

        private void OnWindow(int windowId)
        {
            GenerateWindow();
        }

        string ColorToHex(Color32 color)
        {
            string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
            return hex;
        }

        private void GenerateWindow()
        {
            GUILayout.BeginVertical();
            scrollPos = GUILayout.BeginScrollView(scrollPos, _scrollStyle, GUILayout.Width(680), GUILayout.Height(350));
            GUILayout.BeginVertical();

            try
            {
                var numServos = 0;
                foreach (var p in FlightGlobals.ActiveVessel.parts)
                {
                    var servos = p.FindModulesImplementing<ModuleServo>();
                    if (servos.Any())
                    {
                        numServos++;
                        bool setPos = false;

                        if (showServo.Count < numServos)
                            showServo.Add(true);

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(String.Format("<color=#FFFFFF>[{0}] {1}</color>", numServos,p.partInfo.title), _labelStyle, GUILayout.Width(135));

                        if (showServo[numServos - 1])
                        {
                            if (GUILayout.Button("-", GUILayout.Width(35)))
                                showServo[numServos - 1] = false;
                        }
                        else
                        {
                            if (GUILayout.Button("+", GUILayout.Width(35)))
                                showServo[numServos - 1] = true;
                        }
                        GUILayout.EndHorizontal();

                        if (showServo[numServos - 1])
                        {
                            foreach (var servo in servos)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("", _labelStyle, GUILayout.Width(30));
                                GUILayout.Label(String.Format("{0}", servo.menuName), _labelStyle, GUILayout.Width(150));
                                var goal = GUILayout.TextField(servo.goalValue.ToString("0.00"), 10, GUILayout.Width(50));
                                var tmp = 0f;
                                if (float.TryParse(goal, out tmp))
                                    servo.goalValue = tmp;
                                else if (string.IsNullOrEmpty(goal))
                                    servo.goalValue = 0;

                                GUILayout.Label(String.Format("{0:0.00}", servo.DisplayPosition), _labelStyle, GUILayout.Width(50));
                                if (GUILayout.Button("<->", GUILayout.Width(35)))
                                    servo.ServoSpeed *= -1;
                                if (GUILayout.Button("-0-", GUILayout.Width(35)))
                                    servo.ServoSpeed = 0;
                                if(servo.MoveToGoal)
                                {
                                    if (GUILayout.Button("-?-", GUILayout.Width(35)))
                                        servo.MoveToGoal = false;
                                }
                                else
                                {
                                    if (GUILayout.Button("-G-", GUILayout.Width(35)))
                                        servo.MoveToGoal = true;
                                }
                                GUILayout.Label("", _labelStyle, GUILayout.Width(30));

                                if (GUILayout.Button("-10", GUILayout.Width(35)))
                                    servo.ServoSpeed -= 10;
                                if (GUILayout.Button("-1", GUILayout.Width(35)))
                                    servo.ServoSpeed -= 1;
                                GUILayout.Label("", _labelStyle, GUILayout.Width(5));
                                GUILayout.Label(String.Format("<color=#FFD900>{0:0}</color>", servo.ServoSpeed), _labelStyle, GUILayout.Width(20));
                                GUILayout.Label("", _labelStyle, GUILayout.Width(5));
                                if (GUILayout.Button("+1", GUILayout.Width(35)))
                                    servo.ServoSpeed += 1;
                                if (GUILayout.Button("+10", GUILayout.Width(35)))
                                    servo.ServoSpeed += 10;
                                GUILayout.EndHorizontal();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.StackTrace);
            }
            finally
            {
                GUILayout.EndVertical();
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                GUI.DragWindow();
            }
        }

        internal void OnDestroy()
        {
            if (servoButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(servoButton);
                servoButton = null;
            }
            if (planLogTButton != null)
            {
                planLogTButton.Destroy();
                planLogTButton = null;
            }
        }

        private void InitStyles()
        {
            _windowStyle = new GUIStyle(HighLogic.Skin.window);
            _windowStyle.fixedWidth = 700f;
            _windowStyle.fixedHeight = 400f;
            _labelStyle = new GUIStyle(HighLogic.Skin.label);
            _buttonStyle = new GUIStyle(HighLogic.Skin.button);
            _scrollStyle = new GUIStyle(HighLogic.Skin.scrollView);
            _hasInitStyles = true;
        }
    }
}