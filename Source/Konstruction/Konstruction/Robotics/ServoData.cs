using UnityEngine;

namespace Konstruction
{
    public class ServoData
    {
        public Transform ServoTransform { get; set; }
        public Vector3 CurrentPosition { get; set;}
        public ServoPosition MinRange { get; set; }
        public ServoPosition MaxRange { get; set; }
        public ServoPosition StepAmount { get; set; }
        public bool ChangeX { get; set; }
        public bool ChangeY { get; set; }
        public bool ChangeZ { get; set; }
    }
}