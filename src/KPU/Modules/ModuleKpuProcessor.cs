﻿using System;
using System.Text;
using UnityEngine;

namespace KPU.Modules
{
    [KSPModule("KPU Processor")]
    public class ModuleKpuProcessor : PartModule, IDisposable
    {
        private Processor.Processor mProcessor = null;

        [KSPField()]
        public bool hasLevelTrigger;
        [KSPField()]
        public bool hasLogicOps;
        [KSPField()]
        public bool hasArithOps;
        [KSPField()]
        public int imemWords;

        [KSPField()]
        public bool isRunning = true;

        KPU.UI.WatchWindow mWatchWindow;

        [KSPEvent(name = "EventEdit", guiName = "Edit Program", guiActive = true, guiActiveUnfocused = true)]
        public void EventEdit()
        {
            if (mProcessor == null)
            {
                KPU.Logging.Log("Tried to edit but no mProcessor");
            }
            else
            {
                KPU.Logging.Log("EventEdit");
            }
        }

        [KSPEvent(name = "EventOpen", guiName = "Watch Display", guiActive = true, guiActiveUnfocused = true)]
        public void EventOpen()
        {
            if (mProcessor == null)
            {
                KPU.Logging.Log("Tried to open but no mProcessor");
            }
            else
            {
                if (mWatchWindow == null)
                    mWatchWindow = new KPU.UI.WatchWindow(mProcessor);
                mWatchWindow.Show();
            }
        }

        public override void OnStart(StartState state)
        {
            mProcessor = new Processor.Processor(part, this);
            GameEvents.onVesselChange.Add(OnVesselChange);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            try
            {
                if (HighLogic.fetch && HighLogic.LoadedSceneIsFlight)
                {
                    if (mProcessor == null)
                        mProcessor = new Processor.Processor(part, this);
                    mProcessor.Save(node);
                }

            }
            catch (Exception e) { print(e); }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            try
            {
                if (HighLogic.fetch && HighLogic.LoadedSceneIsFlight)
                {
                    if (mProcessor == null)
                        mProcessor = new Processor.Processor(part, this);
                    mProcessor.Load(node);
                }
            }
            catch (Exception e) { print(e); };
        }

        public override void OnUpdate()
        {
            if (mProcessor != null) mProcessor.OnUpdate();
        }

        public void OnVesselChange(Vessel v)
        {
            if (mWatchWindow != null)
            {
                mWatchWindow.Hide();
            }
        }

        public override string GetInfo()
        {
            var info = new StringBuilder();

            info.AppendLine("Supports Edge-Triggers");
            if (hasLevelTrigger)
                info.AppendLine("Supports Level-Triggers");
            if (hasLogicOps)
                info.AppendLine("Supports Logical Ops");
            if (hasArithOps)
                info.AppendLine("Supports Arithmetic Ops");
            
            return info.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }

        public void Dispose()
        {
            KPU.Logging.Log("ModuleKpuProcessor: Dispose");

            GameEvents.onVesselChange.Remove(OnVesselChange);

            if (mWatchWindow != null)
            {
                mWatchWindow.Hide();
            }
        }
    }
}

