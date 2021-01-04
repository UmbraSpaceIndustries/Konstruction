using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using ModuleWheels;
using UnityEngine;

namespace Konstruction
{
    public class ModuleServo : PartModule
    {
        [KSPField]
        public string wheelTransform = "deployTgt";

        [KSPField] 
        public string menuName = "Servo";

        [KSPField]
        public string mode = "rotate";

        //A series of transforms, their directions, and their ranges
        [KSPField] 
        public string transformConfig;

        //A series of transforms and their deltas
        [KSPField(isPersistant = true)]
        public string transformDeltas;

        [KSPField(isPersistant = true)]
        public float goalValue;

        [KSPField(isPersistant = true)]
        public bool MoveToGoal;

        [KSPField(isPersistant = true)]
        public int GroupBehavior = 0;

        public float ServoSpeed = 0f;
        public List<ServoPosition> currentPositions;
        public List<ServoData> ServoTransforms;

        public float DisplayPosition;
        public float DisplaySpeed;
        public string GoalString;

        [KSPAction("Increase Speed")]
        public void IncreaseAction(KSPActionParam param)
        {
            ServoSpeed += 1;
        }

        [KSPAction("Decrease Speed")]
        public void DecreaseAction(KSPActionParam param)
        {
            ServoSpeed -= 1;
        }


        private float GetTraversalSpeed(float maxPos, float minPos)
        {
            if (ServoSpeed > 0)
            {
                var maxSpeed = (maxPos - DisplayPosition) / DisplaySpeed;
                return Math.Min(ServoSpeed, maxSpeed);
            }
            else
            {
                var maxSpeed = (DisplayPosition - minPos) / DisplaySpeed;
                return Math.Max(ServoSpeed, -maxSpeed);
            }
        }

        private float GetMaxSpeed(float maxPos, float minPos)
        {
            if (ServoSpeed > 0)
            {
                var maxTravel = maxPos -  DisplayPosition;
                var maxSpeed = maxTravel / DisplaySpeed;
                return Math.Min(maxSpeed,ServoSpeed);
            }
            else
            {
                var maxTravel = minPos - DisplayPosition;
                var maxSpeed = maxTravel / DisplaySpeed;
                return Math.Max(maxSpeed, ServoSpeed);
            }
        }

        private void CheckObjects()
        {
            if (currentPositions == null)
            {
                LoadPositions();
                ApplyStartPosition();
                SetGUIValues();
                MonoUtilities.RefreshContextWindows(part);
            }

            if (ServoTransforms == null)
                SetupTransforms();
        }

        private ModuleWheelBase _wheel;
        private Transform _wheelTransform;
        private Vector3 _oldPos;
        private Vector3 _oldRot;

        private void ResetWheelPosition()
        {
            if (_wheel == null || _wheelTransform == null)
                return;

            var dis = (_oldPos - _wheelTransform.position).sqrMagnitude;
            var rot = (_oldRot - _wheelTransform.rotation.eulerAngles).sqrMagnitude;

            if (dis < 0.00001 && rot < 0.00001)
                return;

            _wheel.Wheel.SetWheelTransform(_wheelTransform.position, _wheelTransform.rotation);
            _oldPos = new Vector3(_wheelTransform.position.x, _wheelTransform.position.y, _wheelTransform.position.z);
            _oldRot = new Vector3(_wheelTransform.rotation.eulerAngles.x, _wheelTransform.rotation.eulerAngles.y, _wheelTransform.rotation.eulerAngles.z);

        }

        public void FixedUpdate()
        {
            if (Math.Abs(ServoSpeed) < ResourceUtilities.FLOAT_TOLERANCE)
                return;

            try
            {
                CheckObjects();
                SetGUIValues();

                if (mode == "rotate")
                    ProcessRotator();
                else
                    ProcessTraversal();
                ResetWheelPosition();
            }
            catch (Exception ex)
            {
                print("ERROR in ModuleServo (FixedUpdate) " + ex.Message);
            }
        }

        private void ProcessRotator()
        {
            if (MoveToGoal)
            {
                var maxTravel = Math.Abs(DisplayPosition - goalValue);
                if (maxTravel <= ResourceUtilities.FLOAT_TOLERANCE)
                    return;
                //Normalize goal speed
                var goalSpeed = Math.Abs(maxTravel / DisplaySpeed);
                ServoSpeed = Math.Min(Math.Abs(ServoSpeed), goalSpeed);
                if (DisplayPosition > goalValue)
                    ServoSpeed *= -1;
            }
            else
            {
                var servo = ServoTransforms.First();
                if (servo.ChangeX)
                    ServoSpeed = GetMaxSpeed(servo.MaxRange.x, servo.MinRange.x);
                if (servo.ChangeY)
                    ServoSpeed = GetMaxSpeed(servo.MaxRange.y, servo.MinRange.y);
                if (servo.ChangeZ)
                    ServoSpeed = GetMaxSpeed(servo.MaxRange.z, servo.MinRange.z);
            }
            if (Math.Abs(DisplaySpeed) < ResourceUtilities.FLOAT_TOLERANCE)
                return;

            UpdateRotators();
        }


        private void ProcessTraversal()
        {
            if (MoveToGoal)
            {
                var maxTravel = Math.Abs(DisplayPosition - goalValue);
                if (maxTravel <= ResourceUtilities.FLOAT_TOLERANCE)
                    return;
                //Normalize goal speed
                var goalSpeed = Math.Abs(maxTravel / DisplaySpeed);
                ServoSpeed = Math.Min(Math.Abs(ServoSpeed), goalSpeed);
                if (DisplayPosition > goalValue)
                    ServoSpeed *= -1;
            }
            else
            {
                var servo = ServoTransforms.First();

                if (servo.ChangeX)
                {
                    ServoSpeed = GetTraversalSpeed(servo.MaxRange.x, servo.MinRange.x);
                }
                if (servo.ChangeY)
                {
                    ServoSpeed = GetTraversalSpeed(servo.MaxRange.y, servo.MinRange.y);
                }
                if (servo.ChangeZ)
                {
                    ServoSpeed = GetTraversalSpeed(servo.MaxRange.z, servo.MinRange.z);
                }
            }
            if (Math.Abs(DisplaySpeed) < ResourceUtilities.FLOAT_TOLERANCE)
                return;

            UpdateTranslators();
        }




        private void UpdateTranslators()
        {
            //Transform adjustment time!
            foreach (var servo in ServoTransforms)
            {
                var rot = new ServoPosition
                {
                    x = servo.StepAmount.x * ServoSpeed,
                    y = servo.StepAmount.y * ServoSpeed,
                    z = servo.StepAmount.z * ServoSpeed
                };

                servo.ServoTransform.Translate(new Vector3(rot.x,rot.y,rot.z),Space.Self);

                var cur = currentPositions.First(c => c.TransformName == servo.ServoTransform.name);
                cur.x += rot.x;
                cur.y += rot.y;
                cur.z += rot.z;
                SavePositions();
            }
        }




        private void UpdateRotators()
        {
            //Transform adjustment time!
            foreach (var servo in ServoTransforms)
            {
                var rot = new ServoPosition
                {
                    x = servo.StepAmount.x * ServoSpeed,
                    y = servo.StepAmount.y * ServoSpeed,
                    z = servo.StepAmount.z * ServoSpeed
                };

                servo.ServoTransform.Rotate(new Vector3(rot.x,rot.y,rot.z));

                var cur = currentPositions.First(c => c.TransformName == servo.ServoTransform.name);
                cur.x += rot.x;
                cur.y += rot.y;
                cur.z += rot.z;

                servo.CurrentPosition = new Vector3(cur.x, cur.y, cur.z);
                SavePositions();
            }
        }

        public override void OnStart(StartState state)
        {
            SetupTransforms();
        }

        private void SetupTransforms()
        {
            if (_wheel == null)
                _wheel = part.FindModuleImplementing<ModuleWheelBase>();
            if (_wheelTransform == null)
                _wheelTransform = part.FindModelTransform(wheelTransform);

            ServoTransforms = new List<ServoData>();
            var tList = transformConfig.Split(',');
            for (int i = 0; i < tList.Count(); i += 10)
            {
                var g = part.FindModelTransform(tList[i]);
                var min = new ServoPosition { x = float.Parse(tList[i + 1]), y = float.Parse(tList[i + 2]), z = float.Parse(tList[i + 3])};
                var max = new ServoPosition { x = float.Parse(tList[i + 4]), y = float.Parse(tList[i + 5]), z = float.Parse(tList[i + 6])};
                var rot = new ServoPosition { x = float.Parse(tList[i + 7]), y = float.Parse(tList[i + 8]), z = float.Parse(tList[i + 9])};
                var rx = Math.Abs(rot.x) > ResourceUtilities.FLOAT_TOLERANCE;
                var ry = Math.Abs(rot.y) > ResourceUtilities.FLOAT_TOLERANCE;
                var rz = Math.Abs(rot.z) > ResourceUtilities.FLOAT_TOLERANCE;
                ServoTransforms.Add(new ServoData { ServoTransform = g, MinRange = min, MaxRange = max, StepAmount = rot, ChangeX = rx, ChangeY = ry, ChangeZ = rz });
            }
            LoadPositions();
            ApplyStartPosition();
            SetGUIValues();
            MonoUtilities.RefreshContextWindows(part);
        }

        private void SetGUIValues()
        {
            var pos = currentPositions.First();
            var servo = ServoTransforms.Find(s => s.ServoTransform.transform.name == pos.TransformName);
            if (servo.ChangeX)
            {
                DisplayPosition = pos.x;
                DisplaySpeed = servo.StepAmount.x;
            }
            if (servo.ChangeY)
            {
                DisplayPosition = pos.y;
                DisplaySpeed = servo.StepAmount.y;
            }
            if (servo.ChangeZ)
            {
                DisplayPosition = pos.z;
                DisplaySpeed = servo.StepAmount.z;
            }
        }


        private void SavePositions()
        {
            var config = new StringBuilder("");
            foreach (var p in currentPositions)
            {
                config.Append(",");
                config.Append(p.TransformName);
                config.Append(",");
                config.Append(p.x.ToString("n6"));
                config.Append(",");
                config.Append(p.y.ToString("n6"));
                config.Append(",");
                config.Append(p.z.ToString("n6"));
            }
            transformDeltas = config.ToString().Substring(1);
        }

        public void SetGoal(float goal,float speed)
        {
            goalValue = goal;
            MoveToGoal = true;
            ServoSpeed = speed;
        }

        private void LoadPositions()
        {
            currentPositions = new List<ServoPosition>();
            var tList = transformDeltas.Split(',');
            if (tList.Length >= 4)
            {
                for (int i = 0; i < tList.Count(); i += 4)
                {
                    currentPositions.Add(new ServoPosition
                    {
                        TransformName = tList[i],
                        x = float.Parse(tList[i + 1]),
                        y = float.Parse(tList[i + 2]),
                        z = float.Parse(tList[i + 3])
                    });
                }
            }
            //And we may be missing some...
            foreach (var t in ServoTransforms)
            {
                if (currentPositions.All(p => p.TransformName != t.ServoTransform.transform.name))
                {
                    currentPositions.Add(new ServoPosition
                    {
                        TransformName = t.ServoTransform.name, x = 0f, y = 0f, z = 0f
                    });
                }
            }
        }

        private void ApplyStartPosition()
        {
            foreach (var d in currentPositions)
            {
                var g = part.FindModelTransform(d.TransformName);
                var t = ServoTransforms.Find(s => s.ServoTransform.transform.name == d.TransformName);
                t.CurrentPosition = new Vector3(d.x, d.y, d.z);
                if (mode == "rotate")
                    g.Rotate(new Vector3(d.x,d.y,d.z));
                else if (mode == "translate")
                    g.Translate(new Vector3(d.x,d.y,d.z),Space.Self);
            }
        }
    }
}