using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Konstruction.Utilities
{
    class GeneralUtils
    {
        public static bool IsOnSurface()
        {
            var situation = FlightGlobals.ActiveVessel.situation;
            return situation == Vessel.Situations.LANDED || situation == Vessel.Situations.PRELAUNCH || situation == Vessel.Situations.SPLASHED;
        }
    }
}
