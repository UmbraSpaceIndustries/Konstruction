using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Konstruction
{
    public class ServoPosition
    {
        public string TransformName { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }

    public class ModuleServo : PartModule
    {
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

        public float ServoSpeed = 0f;
        public List<ServoPosition> currentPositions;
        public List<ServoData> ServoTransforms;

        public float DisplayPosition;
        public float DisplaySpeed;

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

        public void FixedUpdate()
        {
            if (Math.Abs(ServoSpeed) < ResourceUtilities.FLOAT_TOLERANCE)
                return;


            if (mode == "rotate")
                UpdateRotators();
            else if (mode == "translate")
                UpdateTranslators();

            var maxTravel = Math.Abs(DisplayPosition - goalValue);
            if (MoveToGoal)
            {
                if (maxTravel <= ResourceUtilities.FLOAT_TOLERANCE)
                    return;
                //Normalize goal speed
                var goalSpeed = Math.Abs(maxTravel / DisplaySpeed);
                ServoSpeed = Math.Min(Math.Abs(ServoSpeed), goalSpeed);
                if (DisplayPosition > goalValue)
                    ServoSpeed *= -1;
            }

        }

        private void UpdateTranslators()
        {
            if (ServoTransforms == null)
                SetupTransforms();
            //If any of the transforms in this servo set are out of range... stop.   This is because
            //multiple modules could be messing with these.
            foreach (var servo in ServoTransforms)
            {
                if (OutOfRange(servo))
                {
                    ServoSpeed = 0f;
                    return;
                }
            }

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
            if(ServoTransforms == null)
                SetupTransforms();
            //If any of the transforms in this servo set are out of range... stop.   This is because
            //multiple modules could be messing with these.
            foreach (var servo in ServoTransforms)
            {
                if (OutOfAngle(servo))
                {
                    ServoSpeed = 0f;
                    return;
                }
            }

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

        private bool OutOfRange(ServoData servo)
        {
            if (servo.ChangeX)
            {
                var newPos = servo.ServoTransform.localPosition.x + (servo.StepAmount.x * ServoSpeed);
                DisplayPosition = newPos;
                DisplaySpeed = servo.StepAmount.x;
                if (newPos > servo.MaxRange.x || newPos < servo.MinRange.x)
                    return true;
            }
            if (servo.ChangeY)
            {
                var newPos = servo.ServoTransform.localPosition.y + (servo.StepAmount.y * ServoSpeed);
                DisplayPosition = newPos;
                DisplaySpeed = servo.StepAmount.y;
                if (newPos > servo.MaxRange.y || newPos < servo.MinRange.y)
                    return true;
            }
            if (servo.ChangeZ)
            {
                var newPos = servo.ServoTransform.localPosition.z + (servo.StepAmount.z * ServoSpeed);
                DisplayPosition = newPos;
                DisplaySpeed = servo.StepAmount.z;
                if (newPos > servo.MaxRange.z || newPos < servo.MinRange.z)
                    return true;
            }
            return false;
        }

        private bool CheckAngle(float step, float angle, float min, float max)
        {
            float change = step * ServoSpeed;
            float oldAngle = NormalizedAngle(angle);
            float newAngle = oldAngle + change;

            if (newAngle > max || newAngle < min)
            {
                return true;
            }

            DisplayPosition = NormalizedAngle(newAngle);
            DisplaySpeed = step;
            return false;
        }

        private float NormalizedAngle(float angle)
        {
            if (angle > 180f)
                return angle -360f;
            return angle;
        }

        private bool OutOfAngle(ServoData servo)
        {
            if (servo.ChangeX)
            {
                return CheckAngle(servo.StepAmount.x, servo.CurrentPosition.x, servo.MinRange.x, servo.MaxRange.x);
            }
            if (servo.ChangeY)
            {
                return CheckAngle(servo.StepAmount.y, servo.CurrentPosition.y, servo.MinRange.y, servo.MaxRange.y);
            }
            if (servo.ChangeZ)
            {
                return CheckAngle(servo.StepAmount.z, servo.CurrentPosition.z, servo.MinRange.z, servo.MaxRange.z);
            }
            return false;
        }

        public override void OnStart(StartState state)
        {
            SetupTransforms();
        }


        private void SetupTransforms()
        {
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
            MonoUtilities.RefreshContextWindows(part);
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
                if (!currentPositions.Any(p=>p.TransformName == t.ServoTransform.transform.name))
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