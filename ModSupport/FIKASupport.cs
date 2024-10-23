﻿using EFT.UI;
using EFT;
using HarmonyLib;
using TarkovVR.Patches.UI;
using UnityEngine;
using TarkovVR.Patches.Misc;
using BepInEx.Configuration;
using Fika.Core;
using EFT.Communications;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using static EFT.HealthSystem.ActiveHealthController;
using static Fika.Core.Coop.Components.CoopHandler;
using System;
using Valve.VR.InteractionSystem;
using Valve.VR;
using Comfort.Common;
using TarkovVR.Source.Settings;


namespace TarkovVR.ModSupport.FIKA
{
    [HarmonyPatch]
    internal static class FIKASupport
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MatchMakerUI), "Awake")]
        private static void SetMatchMakerUI(MatchMakerUI __instance)
        {
            __instance.transform.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            __instance.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            __instance.transform.localPosition = new Vector3(0.117f, -999.7602f, 0.9748f);
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(TarkovApplication), "method_50")]
        private static bool FixExitRaid(TarkovApplication __instance)
        {
            UIPatches.gameUi.transform.parent = null;
            UIPatches.HandleCloseInventory();

            if (UIPatches.notifierUi != null)
                UIPatches.notifierUi.transform.parent = PreloaderUI.Instance._alphaVersionLabel.transform.parent;

            if (UIPatches.extractionTimerUi != null)
                UIPatches.extractionTimerUi.transform.parent = UIPatches.gameUi.transform;
            if (UIPatches.healthPanel != null)
                UIPatches.healthPanel.transform.parent = UIPatches.battleScreenUi.transform;
            if (UIPatches.healthPanel != null)
                UIPatches.stancePanel.transform.parent = UIPatches.battleScreenUi.transform;
            if (UIPatches.battleScreenUi != null)
                UIPatches.battleScreenUi.transform.parent = VRGlobals.commonUi.GetChild(0);


            PreloaderUI.DontDestroyOnLoad(UIPatches.gameUi);
            PreloaderUI.DontDestroyOnLoad(Camera.main.gameObject);
            VRGlobals.inGame = false;
            VRGlobals.menuOpen = true;

            MenuPatches.PositionMainMenuUi();
            return true;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(Fika.Core.Coop.FreeCamera.FreeCameraController), "ShowExtractMessage")]
        private static bool FixExitRaid(Fika.Core.Coop.FreeCamera.FreeCameraController __instance)
        {
            if (FikaPlugin.ShowExtractMessage.Value)
            {
                __instance.extractText = Fika.Core.UI.FikaUIUtils.CreateOverlayText("Press 'B' to extract");
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Fika.Core.Coop.FreeCamera.FreeCameraController), "Update")]
        private static bool PositionExitRaidUI(Fika.Core.Coop.FreeCamera.FreeCameraController __instance)
        {
            if (__instance.extracted) {
                if (__instance.CameraParent != null && Camera.main.transform.parent == null) {
                    Camera.main.transform.parent = __instance.CameraParent.transform;
                }
                PreloaderUI.Instance.transform.position = Camera.main.transform.position + (Camera.main.transform.forward * 0.6f) + (Camera.main.transform.up * 0.2f);
                PreloaderUI.Instance.transform.rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, Camera.main.transform.eulerAngles.z);
                if (Mathf.Abs(SteamVR_Actions._default.LeftJoystick.GetAxis(SteamVR_Input_Sources.Any).y) > VRSettings.GetLeftStickSensitivity())
                {
                    __instance.CameraParent.transform.position += __instance.CameraParent.transform.forward * (SteamVR_Actions._default.LeftJoystick.GetAxis(SteamVR_Input_Sources.Any).y / 10);
                }
                if (Mathf.Abs(SteamVR_Actions._default.LeftJoystick.GetAxis(SteamVR_Input_Sources.Any).x) > VRSettings.GetLeftStickSensitivity())
                {
                    __instance.CameraParent.transform.position += __instance.CameraParent.transform.right * (SteamVR_Actions._default.LeftJoystick.GetAxis(SteamVR_Input_Sources.Any).x / 10);
                }
                if (Mathf.Abs(SteamVR_Actions._default.RightJoystick.GetAxis(SteamVR_Input_Sources.Any).y) > Mathf.Abs(SteamVR_Actions._default.RightJoystick.GetAxis(SteamVR_Input_Sources.Any).x))
                {
                    __instance.CameraParent.transform.position +=  new Vector3(0,SteamVR_Actions._default.RightJoystick.GetAxis(SteamVR_Input_Sources.Any).y / 10,0);
                }
                else if (Mathf.Abs(SteamVR_Actions._default.RightJoystick.GetAxis(SteamVR_Input_Sources.Any).x) > Mathf.Abs(SteamVR_Actions._default.RightJoystick.GetAxis(SteamVR_Input_Sources.Any).y))
                {
                    __instance.CameraParent.transform.rotation = Quaternion.Euler(0, __instance.CameraParent.transform.eulerAngles.y + (SteamVR_Actions._default.RightJoystick.GetAxis(SteamVR_Input_Sources.Any).x * 6),0);
                }

            }
            return true;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(Fika.Core.Coop.Components.CoopHandler), "ProcessQuitting")]
        private static bool OverrideExitRaidButton(Fika.Core.Coop.Components.CoopHandler __instance)
        {
            EQuitState quitState = __instance.GetQuitState();
            if (!SteamVR_Actions._default.ButtonB.stateDown || quitState == EQuitState.NONE || __instance.requestQuitGame)
            {
                return false;
            }
            ConsoleScreen.Log($"{FikaPlugin.ExtractKey.Value} pressed, attempting to extract!");
            Plugin.MyLog.LogInfo((object)$"{FikaPlugin.ExtractKey.Value} pressed, attempting to extract!");
            __instance.requestQuitGame = true;
            CoopGame coopGame = (CoopGame)Singleton<IFikaGame>.Instance;
            if (FikaBackendUtils.IsServer)
            {
                if (Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount > 0 && quitState != EQuitState.NONE)
                {
                    NotificationManagerClass.DisplayWarningNotification(GClass1868.Localized("F_Client_HostCannotExtract", (string)null), (ENotificationDurationType)0);
                    __instance.requestQuitGame = false;
                }
                else if (Singleton<FikaServer>.Instance.NetServer.ConnectedPeersCount == 0 && Singleton<FikaServer>.Instance.timeSinceLastPeerDisconnected > DateTime.Now.AddSeconds(-5.0) && Singleton<FikaServer>.Instance.HasHadPeer)
                {
                    NotificationManagerClass.DisplayWarningNotification(GClass1868.Localized("F_Client_Wait5Seconds", (string)null), (ENotificationDurationType)0);
                    __instance.requestQuitGame = false;
                }
                else
                {
                    ((BaseLocalGame<EftGamePlayerOwner>)coopGame).Stop(Singleton<GameWorld>.Instance.MainPlayer.ProfileId, coopGame.MyExitStatus, ((GClass2430<GClass2429>)(object)((EFT.Player)__instance.MyPlayer).ActiveHealthController).IsAlive ? coopGame.MyExitLocation : null, 0f);
                }
            }
            else
            {
                ((BaseLocalGame<EftGamePlayerOwner>)coopGame).Stop(Singleton<GameWorld>.Instance.MainPlayer.ProfileId, coopGame.MyExitStatus, ((GClass2430<GClass2429>)(object)((EFT.Player)__instance.MyPlayer).ActiveHealthController).IsAlive ? coopGame.MyExitLocation : null, 0f);
            }
            return false;
        }
    }
}