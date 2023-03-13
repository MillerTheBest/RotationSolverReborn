﻿using Dalamud.Game;
using Dalamud.Logging;
using RotationSolver.Commands;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RotationSolver.Updaters;

internal static class MajorUpdater
{
    private static bool IsValid => Service.Conditions.Any() && Service.ClientState.LocalPlayer != null;

    //#if DEBUG
    //    private static readonly Dictionary<int, bool> _valus = new Dictionary<int, bool>();
    //#endif

    private static void FrameworkUpdate(Framework framework)
    {
        if (!IsValid) return;

        //#if DEBUG
        //        //Get changed condition.
        //        string[] enumNames = Enum.GetNames(typeof(Dalamud.Game.ClientState.Conditions.ConditionFlag));
        //        int[] indexs = (int[])Enum.GetValues(typeof(Dalamud.Game.ClientState.Conditions.ConditionFlag));
        //        if (enumNames.Length == indexs.Length)
        //        {
        //            for (int i = 0; i < enumNames.Length; i++)
        //            {
        //                string key = enumNames[i];
        //                bool newValue = Service.Conditions[(Dalamud.Game.ClientState.Conditions.ConditionFlag)indexs[i]];
        //                if (_valus.ContainsKey(i) && _valus[i] != newValue && indexs[i] != 48 && indexs[i] != 27)
        //                {
        //                    Service.ToastGui.ShowQuest(indexs[i].ToString() + " " + key + ": " + newValue.ToString());
        //                }
        //                _valus[i] = newValue;
        //            }
        //        }
        //#endif

        SocialUpdater.UpdateSocial();
        PreviewUpdater.UpdatePreview();
        ActionUpdater.UpdateWeaponTime();

        ActionUpdater.DoAction();

        MacroUpdater.UpdateMacro();

        if (Service.Configuration.UseWorkTask)
        {
            Task.Run(UpdateWork);
        }
        else
        {
            UpdateWork();
        }
    }

    public static void Enable()
    {
        Service.Framework.Update += FrameworkUpdate;
        MovingUpdater.Enable();
    }

    static bool _work;
    private static void UpdateWork()
    {
        if (!IsValid) return;
        if (_work) return;
        _work = true;

        try
        {
            PreviewUpdater.UpdateCastBarState();
            TargetUpdater.UpdateTarget();
            ActionUpdater.UpdateActionInfo();

            RotationUpdater.UpdateRotation();

            TimeLineUpdater.UpdateTimelineAction();
            ActionUpdater.UpdateNextAction();
            RSCommands.UpdateRotationState();
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Worker Exception");
        }

        _work = false;
    }

    public static void Dispose()
    {
        Service.Framework.Update -= FrameworkUpdate;
        PreviewUpdater.Dispose();
        MovingUpdater.Dispose();
    }
}