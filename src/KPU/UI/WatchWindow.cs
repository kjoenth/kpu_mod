﻿using System;
using UnityEngine;

namespace KPU.UI
{
    public class WatchWindow : AbstractWindow
    {
        KPU.Processor.Processor mProcessor;
        private Vector2 mScrollPosition;
        private GUIStyle mKeyStyle, mValueStyle, mHeadingStyle;

        public WatchWindow (KPU.Processor.Processor processor)
            : base(Guid.NewGuid(), "KPU Watch", new Rect(100, 100, 240, 320), WindowAlign.Floating)
        {
            mProcessor = processor;
            mKeyStyle = new GUIStyle(HighLogic.Skin.label)
            {
                fixedWidth = 100,
                fontStyle = FontStyle.Bold,
                fontSize = 11,
            };
            mValueStyle = new GUIStyle(HighLogic.Skin.label)
            {
                fixedWidth = 100,
                fontSize = 11,
            };
            mHeadingStyle = new GUIStyle(HighLogic.Skin.label)
            {
                fixedWidth = 200,
                fontStyle = FontStyle.Bold,
                fontSize = 14,
            };
        }

        public override void Window(int id)
        {
            GUILayout.BeginVertical();
            {
                mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, GUILayout.Width(220), GUILayout.Height(310));
                if (mProcessor == null)
                {
                    GUILayout.Label("I have no Processor!?", mHeadingStyle);
                }
                else if(!mProcessor.hasPower)
                {
                    GUILayout.Label("--NO POWER--", mHeadingStyle);
                }
                else
                {
                    GUILayout.Label("Input Values", mHeadingStyle);
                    if (mProcessor.inputValues != null)
                    {
                        foreach (System.Collections.Generic.KeyValuePair<string, KPU.Processor.InputValue> item in mProcessor.inputValues)
                        {
                            string unit = "";
                            if (mProcessor.inputs.ContainsKey(item.Key))
                                unit = mProcessor.inputs[item.Key].unit;
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(item.Key, mKeyStyle);
                            GUILayout.Label(item.Value.ToString() + unit, mValueStyle);
                            GUILayout.EndHorizontal();
                        }
                    }
                    GUILayout.Label("Output Values", mHeadingStyle);
                    if (mProcessor.outputs != null)
                    {
                        foreach (Processor.IOutputData output in mProcessor.outputs.Values)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(output.name, mKeyStyle);
                            GUILayout.Label(output.value.ToString(), mValueStyle);
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
            base.Window(id);
        }
    }
}

