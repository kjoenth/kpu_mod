﻿using System;
using System.Text;

namespace KPU.Modules
{
    [KSPModule("KPU Sensor Reading")]
    public class ModuleKpuSensor : PartModule
    {
        // Checked for by InputValues in KPU.Processor

        [KSPField()]
        public string sensorType;
        [KSPField()]
        public double sensorRes;
        [KSPField()]
        public string sensorUnit = "";
        [KSPField()]
        public double maxAltitude = 0;
        [KSPField()]
        public string requireBody = "";
        [KSPField()]
        public double sunDegrees = 0;
        [KSPField()]
        public bool requireIP = false;

        [KSPField]
        public String TechRequired = "None";
        private bool Unlocked
        {
            get
            {
                if (master != null && !master.Unlocked)
                    return false;
                return ResearchAndDevelopment.GetTechnologyState(TechRequired) == RDTech.State.Available || TechRequired.Equals("None");
            }
        }

        [KSPField(guiName = "Status", guiActive = false)]
        public string GUI_status = "Inactive";

        private ModuleKpuSensorMaster master { get {
            return part.FindModuleImplementing<ModuleKpuSensorMaster>();
        }}

        public bool isWorking;

        public void FixedUpdate()
        {
            if (vessel == null)
                return;

            Fields["GUI_status"].guiActive = Unlocked;
            if (!Unlocked) return;
            Fields["GUI_status"].guiName = sensorType;
            if (master != null && !master.isWorking)
            {
                GUI_status = master.GUI_status;
                isWorking = false;
                return;
            }
            if (requireBody.Length > 0 && !vessel.orbit.referenceBody.name.Equals(requireBody))
            {
                GUI_status = string.Format("Not at {0}!", requireBody);
                isWorking = false;
                return;
            }
            if (maxAltitude > 0 && vessel.altitude > maxAltitude)
            {
                GUI_status = "Too high!";
                isWorking = false;
                return;
            }
            if (sunDegrees > 0)
            {
                if (!vessel.orbit.referenceBody.name.Equals("Sun") && vessel.directSunlight)
                {
                    Vector3d sun = -FlightGlobals.Bodies[0].position.xzy, toParent = vessel.orbit.pos;
                    double sunAngle = Vector3d.Angle(sun, toParent);
                    if (sunAngle < sunDegrees)
                    {
                        GUI_status = "Blinded by sunlight";
                        isWorking = false;
                        return;
                    }
                }
            }
            if (requireIP)
            {
                if (!vessel.FindPartModulesImplementing<ModuleKpuInertialPlatform>().Exists(m => m.isWorking))
                    isWorking = false;
            }
            isWorking = true;
            GUI_status = "OK";
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();

            info.Append("Sensor: ");
            info.AppendLine(sensorType);
            if (!Unlocked)
                info.AppendFormat("Requires tech: {0}", TechRequired).AppendLine();
            if (sensorRes > 0)
                info.AppendFormat("Resolution: {0:G}{1}", sensorRes, sensorUnit).AppendLine();
            if (maxAltitude > 0)
                info.AppendFormat("Max. Altitude: {0}", Util.formatSI(maxAltitude, "m")).AppendLine();
            if (requireBody.Length > 0)
                info.AppendFormat("In orbit around: {0}", requireBody).AppendLine();
            if (sunDegrees > 0)
                info.AppendFormat("Min. angle to Sun: {0}", Util.formatAngle(sunDegrees)).AppendLine();
            if (requireIP)
                info.AppendLine("Requires Inertial Platform");

            return info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}