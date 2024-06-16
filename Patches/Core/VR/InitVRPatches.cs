﻿using EFT;
using EFT.Animations;
using HarmonyLib;
using RootMotion.FinalIK;
using System.Diagnostics;
using TarkovVR.Patches.UI;
using TarkovVR.Source.Player.Interactions;
using TarkovVR.Source.Player.VR;
using TarkovVR.Source.Player.VRManager;
using TarkovVR.Source.Weapons;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace TarkovVR.Patches.Core.VR
{
    [HarmonyPatch]
    internal class InitVRPatches
    {
        public static Transform rigCollider;
        //------------------------------------------------------------------------------------------------------------------------------------------------------------
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterControllerSpawner), "Spawn")]
        private static void AddVR(CharacterControllerSpawner __instance)
        {
            if (__instance.transform.root.name != "PlayerSuperior(Clone)")
                return;

            if (__instance.transform.root.GetComponent<HideoutPlayer>() != null)
            {
                //if (!VRGlobals.vrPlayer)
                //{
                //    VRGlobals.camHolder.AddComponent<SteamVR_TrackedObject>();
                //    VRGlobals.vrPlayer = VRGlobals.camHolder.AddComponent<HideoutVRPlayerManager>();
                //    VRGlobals.weaponHolder = new GameObject("weaponHolder");
                //    VRGlobals.weaponHolder.transform.parent = VRGlobals.vrPlayer.RightHand.transform;
                //    VRGlobals.vrOpticController = VRGlobals.camHolder.AddComponent<VROpticController>();
                //    VRGlobals.handsInteractionController = VRGlobals.camHolder.AddComponent<HandsInteractionController>();
                //    SphereCollider collider = VRGlobals.camHolder.AddComponent<SphereCollider>();
                //    collider.radius = 0.2f;
                //    collider.isTrigger = true;
                //    VRGlobals.camHolder.layer = 7;
                //    Camera.main.clearFlags = CameraClearFlags.SolidColor;
                //}
            }
            else
            {
                if (!VRGlobals.vrPlayer)
                {
                    VRGlobals.camHolder = new GameObject("camHolder");
                    VRGlobals.vrOffsetter = new GameObject("vrOffsetter");
                    VRGlobals.camRoot = new GameObject("camRoot");
                    if (UIPatches.gameUi)
                    {
                        Plugin.MyLog.LogWarning("Positiing ui from cam init");
                        UIPatches.PositionGameUi(UIPatches.gameUi);
                    }
                    else {
                        Plugin.MyLog.LogWarning("Not set");
                    }
                    VRGlobals.camHolder.transform.parent = VRGlobals.vrOffsetter.transform;
                    //Camera.main.transform.parent = vrOffsetter.transform;
                    //Camera.main.gameObject.AddComponent<SteamVR_TrackedObject>();
                    VRGlobals.vrOffsetter.transform.parent = VRGlobals.camRoot.transform;
                    VRGlobals.camHolder.AddComponent<SteamVR_TrackedObject>();
                    VRGlobals.menuVRManager = VRGlobals.camHolder.AddComponent<MenuVRManager>();
                    VRGlobals.vrPlayer = VRGlobals.camHolder.AddComponent<RaidVRPlayerManager>();
                    VRGlobals.weaponHolder = new GameObject("weaponHolder");
                    VRGlobals.weaponHolder.transform.parent = VRGlobals.vrPlayer.RightHand.transform;
                    VRGlobals.vrOpticController = VRGlobals.camHolder.AddComponent<VROpticController>();
                    VRGlobals.handsInteractionController = VRGlobals.camHolder.AddComponent<HandsInteractionController>();
                    SphereCollider collider = VRGlobals.camHolder.AddComponent<SphereCollider>();
                    collider.radius = 0.2f;
                    collider.isTrigger = true;
                    VRGlobals.camHolder.layer = 7;
                    VRGlobals.menuVRManager.enabled = false;
                }
            }


            if (VRGlobals.backHolster == null)
            {
                VRGlobals.backHolster = new GameObject("backHolsterCollider").transform;
                VRGlobals.backHolster.parent = VRGlobals.camHolder.transform;
                VRGlobals.backCollider = VRGlobals.backHolster.gameObject.AddComponent<BoxCollider>();
                VRGlobals.backHolster.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                VRGlobals.backHolster.localPosition = new Vector3(0.2f, -0.1f, -0.2f);
                VRGlobals.backCollider.isTrigger = true;
                VRGlobals.backHolster.gameObject.layer = 3;
            }
            VRGlobals.inGame = true;
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SolverManager), "OnDisable")]
        private static void AddVRHands(LimbIK __instance)
        {
            //if (__instance.transform.root.name != "PlayerSuperior(Clone)")
            //    return;
            if (__instance.transform.root.name != "PlayerSuperior(Clone)")
                return;

            StackTrace stackTrace = new StackTrace();
            bool isBotPlayer = false;
            foreach (var frame in stackTrace.GetFrames())
            {
                var method = frame.GetMethod();
                var declaringType = method.DeclaringType.FullName;
                var methodName = method.Name;

                // Check for bot-specific methods
                if (declaringType.Contains("EFT.BotSpawner") || declaringType.Contains("GClass732") && methodName.Contains("ActivateBot"))
                {
                    isBotPlayer = true;
                    break;
                }
            }

            // This is a bot player, so do not execute the rest of the code
            if (isBotPlayer)
            {
                return;
            }

            //    //This is for Base HumanSpine3 to stop it doing something, cant remember

            if (__instance.transform.parent.parent.GetComponent<IKManager>() == null)
            {
                VRGlobals.ikManager = __instance.transform.parent.parent.gameObject.AddComponent<IKManager>();
            }

            if (__instance.name == VRGlobals.LEFT_ARM_OBJECT_NAME )
            {
                __instance.enabled = true;
                VRGlobals.ikManager.leftArmIk = __instance;
                if (VRGlobals.vrPlayer)
                    __instance.solver.target = VRGlobals.vrPlayer.LeftHand.transform;
                // Set the weight to 2.5 so when rotating the hand, the wrist rotates as well, showing the watch time
                VRGlobals.leftWrist = __instance.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0);
                //leftWrist.GetComponent<TwistRelax>().weight = 2.5f;
            }
            else if (__instance.name == VRGlobals.RIGHT_ARM_OBJECT_NAME)
            {

                __instance.enabled = true;
                VRGlobals.ikManager.rightArmIk = __instance;
                if (VRGlobals.ikManager.rightHandIK == null)
                    VRGlobals.ikManager.rightHandIK = __instance.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).gameObject;
                if (VRGlobals.vrPlayer)
                    __instance.solver.target = VRGlobals.vrPlayer.RightHand.transform;



            }

        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Don't know why I chose this method for setting the main cam but it works so whatever
        [HarmonyPostfix]
        [HarmonyPatch(typeof(BloodOnScreen), "Start")]
        private static void SetMainCamParent(BloodOnScreen __instance)
        {
            Camera mainCam = __instance.GetComponent<Camera>();
            if (mainCam.name == "FPS Camera")
            {
                Plugin.MyLog.LogWarning("\n\nSetting camera \n\n");
                GameObject uiCamHolder = new GameObject("uiCam");
                uiCamHolder.transform.parent = __instance.transform;
                uiCamHolder.transform.localRotation = Quaternion.identity;
                uiCamHolder.transform.localPosition = Vector3.zero;
                Camera uiCam = uiCamHolder.AddComponent<Camera>();
                uiCam.nearClipPlane = 0.001f;
                uiCam.depth = 1;
                uiCam.cullingMask = 32;
                uiCam.clearFlags = CameraClearFlags.Depth;
                mainCam.transform.parent = VRGlobals.vrOffsetter.transform;
                //mainCam.cullingMask = -1;
                mainCam.nearClipPlane = 0.001f;
                mainCam.farClipPlane = 1000f;
                mainCam.gameObject.AddComponent<SteamVR_TrackedObject>();
                //mainCam.gameObject.GetComponent<PostProcessLayer>().enabled = false;
                //cameraManager.initPos = VRCam.transform.localPosition;
            }

        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerSpring), "Start")]
        private static void SetMwainCamParent(PlayerSpring __instance)
        {
            if (__instance.name == "Base HumanRibcage" && rigCollider == null)
            {
                rigCollider = new GameObject("rigCollider").transform;
                BoxCollider collider = rigCollider.gameObject.AddComponent<BoxCollider>();
                rigCollider.parent = __instance.transform.parent;
                rigCollider.localEulerAngles = Vector3.zero;
                rigCollider.localPosition = Vector3.zero;
                rigCollider.gameObject.layer = 3;
                collider.isTrigger = true;
                collider.size = new Vector3(0.1f, 0.1f, 0.1f);
            }
        }
    }
}
