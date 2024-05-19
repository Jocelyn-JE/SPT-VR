﻿using HarmonyLib;
using UnityEngine;
using RootMotion.FinalIK;
using TarkovVR.Input;
using Valve.VR;
using Aki.Reflection.Patching;
using EFT;
using System.Reflection;
using TarkovVR.cam;
using EFT.InputSystem;
using static EFT.ClientPlayer;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;
using System.Diagnostics;
using System;

using EFT.CameraControl;

using UnityEngine.Rendering;
using EFT.UI;

using EFT.UI.DragAndDrop;





namespace TarkovVR
{
    [HarmonyPatch]
    internal class CamPatches
    {
        const string LEFT_ARM_OBJECT_NAME = "Base HumanLCollarbone";
        const string RIGHT_ARM_OBJECT_NAME = "Base HumanRCollarbone";


        public static Camera VRCam;
        public static CameraManager cameraManager;
        public static GameObject camHolder;
        public static GameObject vrOffsetter;
        public static GameObject camRoot;

        public static Transform playerCam;

        public static GameObject leftHandIK;
        public static GameObject rightHandIK;

        public static LimbIK leftArmIk;
        public static LimbIK rightArmIk;

        public static GameObject weaponHolder;
        public static GameObject oldWeaponHolder;

        public static Player player;
        public static VROpticController vrOpticController;
        public static HandsInteractionController handsInteractionController;

        public static Transform leftWrist;

        private static float MIN_JOYSTICK_AXIS_FOR_MOVEMENT = 0.5f;
        private static bool isAiming = false;
        private static bool isHoldingBreath = false;
        private static bool isSprinting = false;
        private static bool isShooting = false;
        public static bool inGame = false;


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterControllerSpawner), "Spawn")]
        private static void AddVR(CharacterControllerSpawner __instance)
        {
            if (__instance.transform.root.name != "PlayerSuperior(Clone)")
                return;
             Plugin.MyLog.LogWarning("\n\n CharacterControllerSpawner Spawn " + __instance.gameObject + "\n");
            if (camRoot == null) {
                camHolder = new GameObject("camHolder");
                vrOffsetter = new GameObject("vrOffsetter");
                camRoot = new GameObject("camRoot");
                camHolder.transform.parent = vrOffsetter.transform;
                //Camera.main.transform.parent = vrOffsetter.transform;
                //Camera.main.gameObject.AddComponent<SteamVR_TrackedObject>();
                vrOffsetter.transform.parent = camRoot.transform;

                // VRCam = camHolder.AddComponent<Camera>();
                // VRCam.nearClipPlane = 0.001f;
                //camRoot.AddComponent<TarkovVR.Input.Test>();
            }
            if (!cameraManager) { 
                camHolder.AddComponent<SteamVR_TrackedObject>();
                cameraManager = camHolder.AddComponent<CameraManager>();
                weaponHolder = new GameObject("weaponHolder");
                weaponHolder.transform.parent = CameraManager.RightHand.transform;
                vrOpticController = camHolder.AddComponent<VROpticController>();
                handsInteractionController = camHolder.AddComponent<HandsInteractionController>();
            }
            if (backHolster == null) { 
                backHolster = new GameObject("backHolsterCollider").transform;
                backHolster.parent = camHolder.transform;
                backCollider = backHolster.gameObject.AddComponent<BoxCollider>();
                backHolster.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                backHolster.localPosition = new Vector3(0.2f, -0.1f, -0.2f);
                backCollider.isTrigger = true;
                backHolster.gameObject.layer = 3;
            }
            inGame = true;
        }

        public static BattleStancePanel stancePanel;
        public static CharacterHealthPanel healthPanel;
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameUI), "Awake")]
        private static void SetGameUI(GameUI __instance)
        {
            if (!inGame)
                return;
            __instance.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            __instance.transform.localScale = new Vector3(0.0015f, 0.0015f, 0.0015f);
            stancePanel = __instance.BattleUiScreen._battleStancePanel;
            stancePanel.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            stancePanel._battleStances[0].StanceObject.transform.parent.gameObject.active = false;

            healthPanel = __instance.BattleUiScreen._characterHealthPanel;
            healthPanel.transform.localScale = new Vector3(0.20f, 0.20f, 0.20f);
        }




        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryScreen), "TranslateCommand")]
        private static void HandleCloseInventory(InventoryScreen __instance, ECommand command)
        {
            if (!inGame)
                return;
            if (command.IsCommand(ECommand.Escape))
            {
                if (!__instance.Boolean_0)
                {
                    // If the menu is closed get rid of it, there would be better ways to do this but oh well 
                    HandleCloseInventory();
                }
            }
            if (command.IsCommand(ECommand.ToggleInventory)) {
                HandleCloseInventory();
            }
        }
        //private static Material uiTopMaterial;

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(InventoryScreen), "Awake")]
        //private static void HandleCloseIwnventory(InventoryScreen __instance)
        //{
        //    foreach (Transform child in __instance.transform) {
        //        if (child.name == "Left Glow") {
        //            uiTopMaterial = child.GetComponent<UnityEngine.UI.Image>().material;
        //            Plugin.MyLog.LogError("FOUND UI MATERIAL " + uiTopMaterial);
        //        }
        //    }
        //}


        private static void HandleOpenInventory() {
            menuOpen = true;
            cameraManager.enabled = false;
            MenuPatches.cameraManager.enabled = true;
        }

        private static void HandleCloseInventory()
        {
            menuOpen = false;
            cameraManager.enabled = true;
            MenuPatches.cameraManager.enabled = false;
            MenuPatches.commonUi.position = new Vector3(1000, 1000, 1000);
            MenuPatches.preloaderUi.position = new Vector3(1000, 1000, 1000);
        }
            [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionPanel), "Start")]
        private static void DisableUiPointer(ActionPanel __instance)
        {
            __instance._pointer.gameObject.SetActive(false);
        }



        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionPanel), "method_0")]
        private static void PositionInteractableUi(ActionPanel __instance, GClass2805 interactionState)
        {
            if (interactionState == null) {
                cameraManager.interactionUi = null;
            }
            else
            {
                if (cameraManager.interactionUi == null) { 
                    cameraManager.interactionUi = __instance._interactionButtonsContainer;
                    cameraManager.PositionInteractionUI();
                }
                else { 
                    cameraManager.interactionUi = __instance._interactionButtonsContainer;
                }
                cameraManager.interactionUi.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryBlur), "Enable")]
        private static void DisableInvBlur(InventoryBlur __instance)
        {
            __instance.enabled = false;
        }


        // When the grid is being initialized we need to make sure the rotation is 0,0,0 otherwise the grid items don't
        // spawn in because of their weird code.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GridViewMagnifier), "method_3")]
        private static void ReturnCommonUiToZeroRot(GridViewMagnifier __instance)
        {
            __instance.transform.root.rotation = Quaternion.identity;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MainMenuController), "method_17")]
        private static void ReturnFromGameToMainMenu(MainMenuController __instance)
        {
            inGame = false;
            cameraManager.enabled = false;
            MenuPatches.cameraManager.enabled = true;
            MenuPatches.PositionMainMenuUi();
            MenuPatches.FixMainMenuCamera();
            leftArmIk.solver.target = null;
            rightArmIk.solver.target = null;
            menuOpen = false;
            camRoot.transform.eulerAngles = Vector3.zero;
        }

        // Changes to in game when selecting the hideout option in the preloader UI
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MainMenuController), "method_16")]
        private static void SetInGameOnReturnToHideout_Preloader(MainMenuController __instance)
        {
            // Only set inGame if the player is set. If this is the first time going to hideout when starting the game
            // we only want inGame set if everythings been loaded, and if player is set then that means the hideout
            // has already been opened once, and swapping between hideout and the menu is fast now
            if (player) { 
                inGame = true;
                cameraManager.enabled = true;
                MenuPatches.cameraManager.enabled = false;
                leftArmIk.solver.target = CameraManager.LeftHand.transform;
                rightArmIk.solver.target = CameraManager.RightHand.transform;
            }
        }
        // Changes to in game when selecting the hideout option in the main menu Common UI
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuScreen), "method_6")]
        private static void SetInGameOnReturnToHideout_Common(MenuScreen __instance, EMenuType menuType)
        {
            if (menuType == EMenuType.Hideout && player) { 
                inGame = true;
                cameraManager.enabled = true;
                MenuPatches.cameraManager.enabled = false;
                leftArmIk.solver.target = CameraManager.LeftHand.transform;
                rightArmIk.solver.target = CameraManager.RightHand.transform;
            }
        }


        private static bool menuOpen = false;
        // Position inventory in front of player
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GridViewMagnifier), "method_3")]
        private static void PositionInGameInventory(GridViewMagnifier __instance)
        {
            if (!inGame)
                return;
            if (player && !menuOpen) {
                __instance.transform.root.rotation = Quaternion.identity;
                Transform commonUI = __instance.transform.root;
                commonUI.localScale = new Vector3(0.0006f, 0.0006f, 0.0006f);
                Vector3 newUiPos = Camera.main.transform.position + (Camera.main.transform.forward * 0.7f) + (Camera.main.transform.right * -0.75f);
                newUiPos.y = Camera.main.transform.position.y + -0.6f;
                commonUI.position = newUiPos;
                commonUI.LookAt(Camera.main.transform);
                commonUI.Rotate(0, 225, 0);
                Vector3 newRot = commonUI.eulerAngles;
                newRot.x = 0;
                newRot.z = 0;
                commonUI.eulerAngles = newRot;
                HandleOpenInventory();
                if (MenuPatches.preloaderUi) {
                    MenuPatches.preloaderUi.localScale = new Vector3(0.0008f, 0.0008f, 0.0008f);

                    newUiPos = Camera.main.transform.position + (Camera.main.transform.forward * 0.7f);
                    newUiPos.y = Camera.main.transform.position.y + -0.2f;
                    MenuPatches.preloaderUi.position = newUiPos;
                    MenuPatches.preloaderUi.eulerAngles = newRot;

                }

                //if (uiTopMaterial) { 
                //    Plugin.MyLog.LogError("SETTTING UI MATERIAL " + uiTopMaterial);
                //    foreach (CanvasRenderer renderer in commonUI.GetComponentsInChildren<CanvasRenderer>()) {
                //        renderer.SetMaterial(uiTopMaterial,0);
                //    }
                //}
            }
        }

        // Method_1 starts to despawn the grid items if its rotated
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(GridViewMagnifier), "Update")]
        //private static bool StopGridFromHidingItemsThroughUpdate(GridViewMagnifier __instance)
        //{
        //    if (__instance.enabled)
        //    {
        //        //__instance.method_1(calculate: true, forceMagnify: false);
        //        __instance.method_0();
        //    }
        //    return false;
        //}
        // If the canvas roots rotation isn't 0,0,0 the grid/slot items display on an angle
        // so these patches prevent them from being on an angle
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GridView), "method_5")]
        private static void PreventOffAxisGridItemsViews(GridView __instance, ItemView itemView)
        {
            itemView.transform.localEulerAngles = Vector3.zero;
            itemView.MainImage.transform.localEulerAngles = new Vector3(0, 0, itemView.MainImage.transform.localEulerAngles.z);
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SlotView), "method_5")]
        private static void PreventOffAxisSlotItemsViews(SlotView __instance)
        {
            __instance.itemView_0.transform.localEulerAngles = Vector3.zero;
            __instance.itemView_0.MainImage.transform.localEulerAngles = new Vector3(0, 0, __instance.itemView_0.MainImage.transform.localEulerAngles.z);
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(QuickSlotView), "SetItem")]
        private static void PreventOffAxisSlowtItwemsViews(QuickSlotView __instance)
        {
            __instance.ItemView.transform.localEulerAngles = Vector3.zero;
            __instance.ItemView.MainImage.transform.localEulerAngles = new Vector3(0, 0, __instance.ItemView.MainImage.transform.localEulerAngles.z);
        }

        // This code is somehow responsible for determining which items in the stash/inv grid are shown and it shits the bed if
        // the CommonUI rotation isn't 0,0,0 so set it to that before running this code
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GridViewMagnifier), "method_1")]
        private static bool StopGridFromHidingItemsWhenUiRotated(GridViewMagnifier __instance, bool calculate, bool forceMagnify)
        {
            if ((object)__instance.rectTransform_0 == null || (object)__instance._gridView == null || (object)__instance._scrollRect == null)
            {
                return false;
            }
            Vector3 originalRot = __instance.transform.root.eulerAngles;
             __instance.transform.root.eulerAngles = Vector3.zero;
            if (calculate)
            {

                Rect rect = __instance.rectTransform_0.rect;
                Vector3 vector = __instance.rectTransform_0.TransformPoint(rect.position);
                Vector3 vector2 = __instance.rectTransform_0.TransformPoint(rect.position + rect.size) - vector;
                rect = new Rect(vector, vector2);

                if (!forceMagnify && __instance.nullable_0 == rect)
                {
                    __instance.transform.root.eulerAngles = originalRot;
                    return false;
                }
                __instance.nullable_0 = rect;
            }
            if (__instance.nullable_0.HasValue)
            {
                __instance._gridView.MagnifyIfPossible(__instance.nullable_0.Value, forceMagnify);
            }
            __instance.transform.root.eulerAngles = originalRot;
            return false;
        }


        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(GridViewMagnifier), "method_2")]
        //private static bool StopGridFromHidingItemsWhenMovingUi(GridViewMagnifier __instance, Vector2 position)
        //{
        //    if (!position.Equals(__instance.vector2_0))
        //    {
        //        __instance.vector2_0 = position;
        //    }
        //    return false;
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(InventoryScreen), "Show", new Type[] { typeof(GClass2907) })]
        //private static void PositionInvScreen(InventoryScreen __instance)
        //{
        //    Transform commonUI = __instance.transform.root;
        //    commonUI.localScale = new Vector3(0.0006f, 0.0006f, 0.0006f);
        //    commonUI.position = Camera.main.transform.position + (Camera.main.transform.forward * cameraManager.x) + (Camera.main.transform.up * cameraManager.y) + (Camera.main.transform.right * cameraManager.z);
        //    commonUI.LookAt(Camera.main.transform);
        //    commonUI.Rotate(0, cameraManager.ry, 0);
        //    Vector3 newRot = commonUI.eulerAngles;
        //    newRot.x = 0;
        //    commonUI.eulerAngles = newRot;
        //}


        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(ActionPanel), "Display")]
        //private static void SetGamweUwI(ActionPanel __instance)
        //{
        //    Plugin.MyLog.LogError("Display ");
        //}

        public static Transform backHolster;
        public static BoxCollider backCollider;
        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(PlayerBones), "Awake")]
        //private static void SetupBackHolsterCollision(PlayerBones __instance) {
        //    backHolster = new GameObject("backHolsterCollider").transform;
        //    backHolster.parent = __instance.;
        //    backCollider = backHolster.gameObject.AddComponent<BoxCollider>();
        //    backHolster.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        //    backHolster.localPosition = new Vector3(-0.1f,-0.2f,0);
        //    backCollider.isTrigger = true;
        //    backHolster.gameObject.layer = LayerMask.NameToLayer("Interactive");
        //    backHolster.parent.gameObject.layer = LayerMask.NameToLayer("Interactive");
        //    backHolster.gameObject.tag = "handCollider";

        //}


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

            if (__instance.transform.parent.parent.GetComponent<Test>() == null)
            {
                __instance.transform.parent.parent.gameObject.AddComponent<Test>();
            }


            if (__instance.name == LEFT_ARM_OBJECT_NAME)
            {


                __instance.enabled = true;
                leftArmIk = __instance;
                __instance.solver.target = CameraManager.LeftHand.transform;
                // Set the weight to 2.5 so when rotating the hand, the wrist rotates as well, showing the watch time
                leftWrist = __instance.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0);
                leftWrist.GetComponent<TwistRelax>().weight = 2.5f;
            }
            else if (__instance.name == RIGHT_ARM_OBJECT_NAME)
            {

                __instance.enabled = true;
                rightArmIk = __instance;
                if (rightHandIK == null)
                    rightHandIK = __instance.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0).gameObject;
                __instance.solver.target = CameraManager.RightHand.transform;



            }

        }
        private static Transform emptyHands;


        [HarmonyPostfix]
        [HarmonyPatch(typeof(EFT.Player.EmptyHandsController), "Spawn")]
        private static void ResetWeaponOnEquipHands(EFT.Player.EmptyHandsController __instance)
        {
            if (__instance._player.IsAI)
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
            if (isBotPlayer)
            {
                // This is a bot player, so do not execute the rest of the code
                return;
            }
            firearmController = null;
            emptyHands = __instance.ControllerGameObject.transform;
            player = __instance._player;

            if (oldWeaponHolder && weaponHolder.transform.childCount > 0)
            {
                Plugin.MyLog.LogWarning("\n\nAAAAAAAAAAA : " + __instance._player.gameObject + " \n");
                rightArmIk.solver.target = CameraManager.RightHand.transform;
                Transform weaponRoot = weaponHolder.transform.GetChild(0);
                weaponRoot.parent = oldWeaponHolder.transform;
                weaponRoot.localPosition = Vector3.zero;
                oldWeaponHolder = null;
            }


        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EFT.Player.FirearmController), "IEventsConsumerOnWeapOut")]
        private static void ReturnWeaponToOriginalParentOnChange(EFT.Player.FirearmController __instance)
        {
            if (__instance._player.IsAI)
                return;

            Plugin.MyLog.LogWarning("IEventsConsumerOnWeapOut " + __instance + "   |    " +  __instance.WeaponRoot + "   |    " + __instance.WeaponRoot.parent);

            // Check if a weapon is currently equipped, if that weapon isn the same as the one trying to be equipped, and that the weaponHolder actually has something there
            if (oldWeaponHolder && weaponHolder == __instance.WeaponRoot.parent.gameObject && weaponHolder.transform.childCount > 0)
            {
                Plugin.MyLog.LogWarning("\n\n Init ball calc 1 \n\n");
                rightArmIk.solver.target = CameraManager.RightHand.transform;
                Transform weaponRoot = weaponHolder.transform.GetChild(0);
                weaponRoot.parent = oldWeaponHolder.transform;
                weaponRoot.localPosition = Vector3.zero;
                oldWeaponHolder = null;
            }
        }

        private static EFT.Player.FirearmController firearmController;
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EFT.Player.FirearmController), "InitBallisticCalculator")]
        private static void MoveWeaponToIKHands(EFT.Player.FirearmController __instance)
        {
            if (__instance._player.IsAI)
                return;

            firearmController = __instance;
            player = __instance._player;
            emptyHands = __instance.ControllerGameObject.transform;
            Plugin.MyLog.LogError("\n\n" + oldWeaponHolder + "\n\n");
            scope = __instance.gclass1555_0.sightModVisualControllers_0[0].transform;
            // Check if a weapon is currently equipped, if that weapon isn't the same as the one trying to be equipped, and that the weaponHolder actually has something there
            //if (oldWeaponHolder && oldWeaponHolder != __instance.WeaponRoot.parent.gameObject && weaponHolder.transform.childCount > 0)
            //{
            //    Plugin.MyLog.LogWarning("\n\n Init ball calc 1 \n\n");
            //    rightArmIk.solver.target = CameraManager.RightHand.transform;
            //    Transform weaponRoot = weaponHolder.transform.GetChild(0);
            //    weaponRoot.parent = oldWeaponHolder.transform;
            //    weaponRoot.localPosition = Vector3.zero;
            //    oldWeaponHolder = null;
            //}

            if (rightArmIk && oldWeaponHolder == null)
            {
                Plugin.MyLog.LogWarning("\n\n Init ball calc 2 SET RIGHT HAND \n\n");
                // Weapon_root goes in weapon holder, holder goes in VR right hand, set rot of holder to 15,270,90
                // pos to 0.141 0.0204 -0.1003
                // Pistols rot: 7.8225 278.6073 88.9711  pos: 0.3398 -0.0976 -0.1754

                CameraManager.leftHandGunIK = __instance.HandsHierarchy.Transforms[11];
                //weaponRightHandIKPositioner.gameObject.active = false;
                //Positioner positioner = weaponRightHandIKPositioner.gameObject.AddComponent<Positioner>();
                rightArmIk.solver.target = null;
                //Positioner positioner = rightHandIK.AddComponent<Positioner>();
                //positioner.target = rightHandIK.transform;

                oldWeaponHolder = __instance.WeaponRoot.parent.gameObject;
                __instance.WeaponRoot.transform.parent = weaponHolder.transform;
                //__instance.WeaponRoot.localPosition = Vector3.zero;
                weaponHolder.transform.localRotation = Quaternion.Euler(15, 275, 90);
                if (!__instance.WeaponRoot.gameObject.GetComponent<Test>())
                    __instance.WeaponRoot.gameObject.AddComponent<Test>();
                if (__instance.Weapon.WeapClass == "pistol")
                    weaponHolder.transform.localPosition = new Vector3(0.341f, 0.0904f, -0.0803f);
                else if (__instance.Weapon.WeapClass == "marksmanRifle")
                    weaponHolder.transform.localPosition = new Vector3(0.241f, 0.0204f, -0.1303f);
                else
                    weaponHolder.transform.localPosition = new Vector3(0.141f, 0.0204f, -0.1303f);
                //else if (__instance.Weapon.WeapClass == "assaultRifle" || __instance.Weapon.WeapClass == "assaultCarbine" || __instance.Weapon.WeapClass == "smg" || __instance.Weapon.WeapClass == "sniperRifle" || __instance.Weapon.WeapClass == "marksmanRifle")
                //    weaponHolder.transform.localPosition = new Vector3(0.141f, 0.0204f, -0.1303f);
                //    //weaponHolder.transform.localPosition = new Vector3(0.111f, 0.0204f, -0.1003f);
                //else if (__instance.Weapon.WeapClass == "smg" || __instance.Weapon.WeapClass == "sniperRifle" || __instance.Weapon.WeapClass == "marksmanRifle")
                //    weaponHolder.transform.localPosition = new Vector3(0.141f, 0.0204f, -0.1303f);
                //else
                //    weaponHolder.transform.localPosition = new Vector3(0.241f, 0.0204f, -0.1003f);
            }


            // 26.0009 320.911 103.7912
            // -0.089 -0.0796 -0.1970 
        }

        private static Transform scope;
        private static bool isScrolling;

        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GClass1765), "UpdateInput")]
        private static bool BlockLookAxis(GClass1765 __instance, ref List<ECommand> commands, ref float[] axis, ref float deltaTime)
        {
            // 14: Shoot/F
            // 28: Open Inv/Tab
            // 13: Mousewheel ??
            // 2 & 62: Left Click
            // 3 & 63: Right Click
            // 6:  Middle mouse
            // 29: Jump/Space
            // 22: Crouch/C

            // 0:  Lean Right/E
            // 1:  Lean Left/Q
            // 24: Forward/W
            // 25: Backwards/S
            // 64: Right/D
            // 65: Left/A
             bool isAiming = false;


            if (__instance.ginterface141_0 != null)
            {
                for (int i = 0; i < __instance.ginterface141_0.Length; i++)
                {
                    __instance.ginterface141_0[i].Update();
                    //if (__instance.ginterface141_0 [i].GetValue() != 0)
                    //Plugin.MyLog.LogWarning(i + ": " + __instance.ginterface141_0 [i].GetValue() + "\n");

                }
            }

            // ginterface141_1 Has two elements, scroll up and down
            if (__instance.ginterface141_1 != null)
            {
                for (int j = 0; j < __instance.ginterface141_1.Length; j++)
                {
                    __instance.ginterface141_1[j].Update();
                    //if (__instance.ginterface141_1[j].GetValue() != 0)
                    //Plugin.MyLog.LogError(j + ": " + __instance.ginterface141_1[j].GetValue() + "\n");
                }
            }
            if (__instance.gclass1760_0 != null)
            {
                if (commands.Count > 0)
                {
                    commands.Clear();
                }
                for (int k = 0; k < __instance.gclass1760_0.Length; k++)
                {
                    __instance.ecommand_0 = __instance.gclass1760_0[k].UpdateCommand(deltaTime);

                    if (menuOpen)
                    {

                    }
                    else { 
                        // 62: Jump
                        if (k == 62 && (!cameraManager || !cameraManager.interactionUi || !cameraManager.interactionUi.gameObject.active) && SteamVR_Actions._default.RightJoystick.GetAxis(SteamVR_Input_Sources.Any).y > 0.925f)
                            __instance.ecommand_0 = EFT.InputSystem.ECommand.Jump;
                        // 57: Sprint
                        else if (k == 57)
                        {
                            if (SteamVR_Actions._default.ClickLeftJoystick.GetStateDown(SteamVR_Input_Sources.Any))
                            {
                                if (!isSprinting)
                                    __instance.ecommand_0 = EFT.InputSystem.ECommand.ToggleSprinting;
                                else
                                    __instance.ecommand_0 = EFT.InputSystem.ECommand.EndSprinting;

                            }
                            else if (isSprinting && SteamVR_Actions._default.LeftJoystick.GetAxis(SteamVR_Input_Sources.Any).y < MIN_JOYSTICK_AXIS_FOR_MOVEMENT)
                                __instance.ecommand_0 = EFT.InputSystem.ECommand.EndSprinting;
                        }
                        // 52: Reload
                        else if (k == 52 && SteamVR_Actions._default.ButtonX.GetStateDown(SteamVR_Input_Sources.Any))
                            __instance.ecommand_0 = EFT.InputSystem.ECommand.ReloadWeapon;
                        // 39: Aim
                        else if (k == 39 && firearmController)
                        {
                            float angle = 100f;
                            Vector3 directionToScope = scope.transform.position - camHolder.transform.position;
                            directionToScope = directionToScope.normalized;
                            angle = Vector3.Angle(camHolder.transform.forward, directionToScope);
                            isAiming = firearmController.IsAiming;
                            if (!isAiming && angle <= 20f)
                            {
                                __instance.ecommand_0 = EFT.InputSystem.ECommand.ToggleAlternativeShooting;
                            }
                            else if (isAiming && angle > 20f)
                            {
                                __instance.ecommand_0 = EFT.InputSystem.ECommand.EndAlternativeShooting;
                                CameraManager.smoothingFactor = 20f;
                            }
                            //if (!isAiming && SteamVR_Actions._default.LeftTrigger.GetAxis(SteamVR_Input_Sources.Any) > 0.5f)
                            //{
                            //    __instance.ecommand_0 = EFT.InputSystem.ECommand.ToggleAlternativeShooting;
                            //    isAiming = true;
                            //}
                            //else if (isAiming && SteamVR_Actions._default.LeftTrigger.GetAxis(SteamVR_Input_Sources.Any) < 0.5f)
                            //{
                            //    __instance.ecommand_0 = EFT.InputSystem.ECommand.EndAlternativeShooting;
                            //    isAiming = false;
                            //}
                        }
                        // 38: Shooting
                        else if (k == 38)
                        {
                            if (!isShooting && SteamVR_Actions._default.RightTrigger.GetAxis(SteamVR_Input_Sources.Any) > 0.5f)
                            {
                                __instance.ecommand_0 = EFT.InputSystem.ECommand.ToggleShooting;
                                isShooting = true;
                            }
                            else if (isShooting && SteamVR_Actions._default.RightTrigger.GetAxis(SteamVR_Input_Sources.Any) < 0.5f)
                            {
                                __instance.ecommand_0 = EFT.InputSystem.ECommand.EndShooting;
                                isShooting = false;
                            }
                        }
                        else if (k == 13)
                        {
                            if (!isScrolling && SteamVR_Actions._default.RightJoystick.GetAxis(SteamVR_Input_Sources.Any).y > 0.5f)
                            {
                                __instance.ecommand_0 = EFT.InputSystem.ECommand.ScrollPrevious;
                                isScrolling = true;
                            }
                            else if (!isScrolling && SteamVR_Actions._default.RightJoystick.GetAxis(SteamVR_Input_Sources.Any).y < -0.5f)
                            {
                                __instance.ecommand_0 = EFT.InputSystem.ECommand.ScrollNext;
                                isScrolling = true;
                            }
                            else if (SteamVR_Actions._default.RightJoystick.GetAxis(SteamVR_Input_Sources.Any).y > -0.5f && SteamVR_Actions._default.RightJoystick.GetAxis(SteamVR_Input_Sources.Any).y < 0.5f)
                                isScrolling = false;
                        }
                        // 78: breathing
                        else if (k == 78)
                        {
                            if (!isHoldingBreath && isAiming && SteamVR_Actions._default.LeftTrigger.GetAxis(SteamVR_Input_Sources.Any) > 0.5f)
                            {
                                __instance.ecommand_0 = EFT.InputSystem.ECommand.ToggleBreathing;
                                isHoldingBreath = true;
                                CameraManager.smoothingFactor = scopeSensitivity * 75f;
                            }
                            else if (isHoldingBreath && (SteamVR_Actions._default.LeftTrigger.GetAxis(SteamVR_Input_Sources.Any) < 0.5f || !isAiming))
                            {
                                __instance.ecommand_0 = EFT.InputSystem.ECommand.EndBreathing;
                                isHoldingBreath = false;
                                CameraManager.smoothingFactor = 20f;
                            }

                            //if (SteamVR_Actions._default.ClickRightJoystick.GetStateDown(SteamVR_Input_Sources.Any))
                            //    __instance.ecommand_0 = EFT.InputSystem.ECommand.ToggleBreathing;
                            //else if (SteamVR_Actions._default.ClickRightJoystick.GetStateUp(SteamVR_Input_Sources.Any))
                            //    __instance.ecommand_0 = EFT.InputSystem.ECommand.EndBreathing;
                        }
                        else if (k == 65 && handsInteractionController && handsInteractionController.swapWeapon)
                        {
                            if (player && player.ActiveSlot.ID == "FirstPrimaryWeapon")
                                __instance.ecommand_0 = EFT.InputSystem.ECommand.SelectSecondPrimaryWeapon;
                            else
                                __instance.ecommand_0 = EFT.InputSystem.ECommand.SelectFirstPrimaryWeapon;
                        }
                        if (k == 1 && isAiming) {
                            if (vrOpticController.swapZooms)
                            {
                                __instance.ecommand_0 = EFT.InputSystem.ECommand.ChangeScopeMagnification;
                                vrOpticController.swapZooms = false;
                            }
                        }
                        
                    }
                    // 50: Interact
                    if (k == 50)
                    {
                        if (SteamVR_Actions._default.ButtonA.GetStateDown(SteamVR_Input_Sources.Any))
                            __instance.ecommand_0 = EFT.InputSystem.ECommand.BeginInteracting;
                        else if (SteamVR_Actions._default.ButtonA.GetStateUp(SteamVR_Input_Sources.Any))
                            __instance.ecommand_0 = EFT.InputSystem.ECommand.EndInteracting;
                    }
                    // 61: Toggle inv
                    else if (k == 61 && SteamVR_Actions._default.ButtonY.GetStateDown(SteamVR_Input_Sources.Any))
                        __instance.ecommand_0 = EFT.InputSystem.ECommand.ToggleInventory;
                    else if (k == 95 && SteamVR_Actions._default.ButtonB.GetStateDown(SteamVR_Input_Sources.Any))
                        __instance.ecommand_0 = EFT.InputSystem.ECommand.Escape;


                    // 0: ChangeAimScope
                    // 1: ChangeAimScopeMagnification
                    // 5: CheckAmmo??
                    // 9: NextWalkPose - Uncrouching Upwards
                    // 10: PreviousWalkPose - Uncrouching Down
                    // 34: WatchTimerAndExits
                    // 41: ToggleGoggles
                    // 47: Tactical - Toggle tactical device like flashlights I think
                    // 48: Next - Scroll next, walk louder
                    // 48: Previous - Scroll previous, walk quieter
                    // 51: Throw grenade
                    // 54: Shooting mode - Semi or auto
                    // 55: Check chamber
                    // 56: Prone
                    // 58: Duck - Full crouch
                    // 63: Knife
                    // 64: PrimaryWeaponFirst
                    // 65: PrimaryWeaponSecond
                    // 66: SecondaryWeapon
                    // 67-73: Quick slots
                    // 93-94: Enter
                    // 95: Escape
                    //__instance.ecommand_0 = SteamVR_Actions._default.ButtonA.GetStateDown(SteamVR_Input_Sources.Any);

                    if (__instance.ecommand_0 != 0)
                    {
                        commands.Add(__instance.ecommand_0);
                        //Plugin.MyLog.LogError(k + ": " + (__instance.gclass1760_0[k] as GClass1802).GameKey + "\n");
                    }
                    //if (__instance.gclass1760_0[k].GetInputCount() != 0)
                    //    Plugin.MyLog.LogWarning(i + ": " + __instance.ginterface141_0 [i].GetValue() + "\n");
                }
            }

            for (int l = 0; l < axis.Length; l++)
            {
                axis[l] = 0f;
            }

            if (__instance.gclass1761_1 == null)
            {
                return false;
            }
            if (cameraManager && !menuOpen) { 
                for (int m = 0; m < __instance.gclass1761_1.Length; m++)
                {
                    if (Mathf.Abs(axis[__instance.gclass1761_1[m].IntAxis]) < 0.0001f)
                    {

                        axis[__instance.gclass1761_1[m].IntAxis] = __instance.gclass1761_1[m].GetValue();
                    }
                    if (m == 3)
                        axis[__instance.gclass1761_1[m].IntAxis] = 0;
                    else if (m == 2)
                    {
                        if (Mathf.Abs(SteamVR_Actions._default.RightJoystick.axis.y) < 0.75f)
                            axis[__instance.gclass1761_1[m].IntAxis] = SteamVR_Actions._default.RightJoystick.axis.x * 8;
                        else
                            axis[__instance.gclass1761_1[m].IntAxis] = 0;
                        if (camRoot != null)
                            camRoot.transform.Rotate(0, axis[__instance.gclass1761_1[m].IntAxis], 0);
                    }
                    else if (m == 0 && Mathf.Abs(SteamVR_Actions._default.LeftJoystick.axis.x) > MIN_JOYSTICK_AXIS_FOR_MOVEMENT)
                        axis[__instance.gclass1761_1[m].IntAxis] = SteamVR_Actions._default.LeftJoystick.axis.x;
                    else if (m == 1 && Mathf.Abs(SteamVR_Actions._default.LeftJoystick.axis.y) > MIN_JOYSTICK_AXIS_FOR_MOVEMENT)
                        axis[__instance.gclass1761_1[m].IntAxis] = SteamVR_Actions._default.LeftJoystick.axis.y;
                    //else if (leftArmIk)
                    //{
                    //    Vector3 headsetPos = Camera.main.transform.position;
                    //    Vector3 playerBodyPos = leftArmIk.transform.root.position + CameraManager.headOffset;
                    //    headsetPos.y = 0;
                    //    playerBodyPos.y = 0;
                    //    float distanceBetweenBodyAndHead = Vector3.Distance(playerBodyPos, headsetPos);
                    //    if (distanceBetweenBodyAndHead >= 0.325 || (matchingHeadToBody && distanceBetweenBodyAndHead > 0.05))
                    //    {
                    //        // Add code for headset to body difference
                    //        matchingHeadToBody = true;
                    //        float moveSpeed = 0.5f;
                    //        Vector3 newPosition = Vector3.MoveTowards(leftArmIk.transform.root.position, headsetPos, moveSpeed * Time.deltaTime);
                    //        Vector3 movementDelta = newPosition - leftArmIk.transform.root.position; // The actual movement vector
                    //        leftArmIk.transform.root.position = newPosition;
                    //        // Now, counteract this movement for vrOffsetter to keep the camera stable in world space
                    //        if (vrOffsetter != null && oldWeaponHolder) // Ensure vrOffsetter is assigned
                    //        {
                    //            Vector3 localMovementDelta = vrOffsetter.transform.parent.InverseTransformVector(movementDelta);
                    //            cameraManager.initPos += localMovementDelta; // Apply inverse local movement to vrOffsetter
                    //        }
                    //    }
                    //    else
                    //        matchingHeadToBody = false;
                    //}
                }
                if (inGame && player)
                {
                    // Base Height - the height at which crouching begins.
                    float baseHeight = cameraManager.initPos.y * 0.90f; // 90% of init height
                                                                       // Floor Height - the height at which full prone is achieved.
                    float floorHeight = cameraManager.initPos.y * 0.40f; // Significant crouch/prone

                    // Current height position normalized between baseHeight and floorHeight.
                    float normalizedHeightPosition = (Camera.main.transform.localPosition.y - floorHeight) / (baseHeight - floorHeight);

                    // Ensure the normalized height is within 0 (full crouch/prone) and 1 (full stand).
                    float crouchLevel = Mathf.Clamp(normalizedHeightPosition, 0, 1);



                    // Handling prone based on crouchLevel instead of raw height differences.
                    if (normalizedHeightPosition < -0.2 && player.MovementContext.CanProne) // Example threshold for prone
                        player.MovementContext.IsInPronePose = true;
                    else
                        player.MovementContext.IsInPronePose = false;

                    // Debug or apply the crouch level
                    //Plugin.MyLog.LogError("Crouch Level: " + crouchLevel + " " + normalizedHeightPosition);
                    player.MovementContext._poseLevel = crouchLevel;

                    //player.MovementContext._poseLevel = crouchLevel;
                }
            }
            //Plugin.MyLog.LogWarning("\n");

            return false;
            //if (__instance.gclass1761_1 == null)
            //{
            //    return;
            //}
            //for (int m = 0; m < __instance.gclass1761_1.Length; m++)
            //{
            //    if (Mathf.Abs(axis[__instance.gclass1761_1[m].IntAxis]) < 0.0001f)
            //    {
            //        axis[__instance.gclass1761_1[m].IntAxis] = 0;
            //    }
            //}

        }
        public static float crouchThreshold = 0.2f; // Height difference before crouching starts
        public static float maxCrouchDifference = 0.1f; // Maximum height difference representing full crouch

        // NOTE: Currently arm stamina lasts way too long, turn it down maybe, or maybe not since the account I'm using has maxed stats
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GClass601), "Process")]
        private static bool OnlyConsumeArmStamOnHoldBreath(GClass601 __instance)
        {
            if (isHoldingBreath) return true;
            return false;
        }



        [HarmonyPrefix]
        [HarmonyPatch(typeof(VolumetricLightRenderer), "OnPreRender")]
        private static bool PatchVolumetricLightingToVR(VolumetricLightRenderer __instance)
        {
            if (UnityEngine.XR.XRSettings.enabled && __instance.camera_0 != null)
            {
                __instance.method_3();

                Camera.StereoscopicEye eye = (Camera.StereoscopicEye)Camera.current.stereoActiveEye;

                Matrix4x4 viewMatrix = __instance.camera_0.GetStereoViewMatrix(eye);
                Matrix4x4 projMatrix = __instance.camera_0.GetStereoProjectionMatrix(eye);
                projMatrix = GL.GetGPUProjectionMatrix(projMatrix, renderIntoTexture: true);
                Matrix4x4 combinedMatrix = projMatrix * viewMatrix;
                __instance.matrix4x4_0 = combinedMatrix;
                __instance.method_4();
                __instance.method_6();
                
                for (int i = 0; i < VolumetricLightRenderer.list_0.Count; i++)
                {
                    VolumetricLightRenderer.list_0[i].VolumetricLightPreRender(__instance, __instance.matrix4x4_0);
                }
                __instance.commandBuffer_0.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);
                __instance.method_5();

                return false;
            }
            return true; 
        }



        private static float scopeSensitivity = 0;

        //NOTE:::::::::::::: Height over bore is the reason why close distances shots aren't hitting, but further distance shots SHOULD be fine - test this

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EFT.CameraControl.OpticComponentUpdater), "CopyComponentFromOptic")]
        private static void SetOpticCamFoV(EFT.CameraControl.OpticComponentUpdater __instance, OpticSight opticSight)
        {
            // NOTE::: I don't think FOV matters at all really for 1x if its not a scope, e.g. collimators or the thermal sights, so all fovs can probs default to 26/27
            //StackTrace stackTrace = new StackTrace();
            //Plugin.MyLog.LogWarning(stackTrace.ToString()); 
            //1x    27 FoV  parent/parent/scope_30mm_s&b_pm_ii_1_8x24(Clone)
            //1x    15 fov parent/parent/scope_all_sig_sauer_echo1_thermal_reflex_sight_1_2x_30hz(Clone) parent/scope_all_torrey_pines_logic_t12_w_30hz(Clone)
            //1x    26 FOV for PARENT/PARENT/scope_30mm_eotech_vudu_1_6x24(Clone)
            //1.5x  18 FOV  parent/scope_g36_hensoldt_hkv_single_optic_carry_handle_1,5x(Clone)
            //1.5x  14 FOV parent/scope_aug_steyr_rail_optic_1,5x(Clone)
            //1.5x  15 FOV parent/scope_aug_steyr_stg77_optic_1,5x
            //2x    4 fov   parent/parent/scope_all_sig_sauer_echo1_thermal_reflex_sight_1_2x_30hz(Clone)
            //2x    11 FOV parent/scope_all_monstrum_compact_prism_scope_2x32(Clone)
            //3x    7.5 FOV  parent/scope_g36_hensoldt_hkv_carry_handle_3x(Clone) parent/3
            //3x    7.6 FoV parent/parent/scope_base_kmz_1p59_3_10x(Clone) parent/parent/scope_all_ncstar_advance_dual_optic_3_9x_42(Clone)
            //3x    9 FOV parent/scope_base_npz_1p78_1_2,8x24(Clone)
            //3x    12 FOV parent/parent/scope_34mm_s&b_pm_ii_3_12x50(Clone)
            //3.5x  6   FOV parent/scope_base_progress_pu_3,5x(Clone)
            //3.5x  6.5 FOV parent/scope_dovetail_npz_nspum_3,5x(Clone)
            //3.5x  7.5 FOV parent/scope_all_swampfox_trihawk_prism_scope_3x30(Clone)
            //3.5x  7 FOV parent/scope_base_trijicon_acog_ta11_3,5x35(Clone)
            //16x   1.2 FoV
            //16x   1 FOV parent/parent/scope_34mm_nightforce_atacr_7_35x56(Clone)


            /////2.5x  10 FOV parent/scope_base_primary_arms_compact_prism_scope_2,5x(Clone)
            //////6x    3.2 FOV 
            ///////10x   2.5 FOV parent/parent/scope_base_kmz_1p59_3_10x(Clone)
            //////20x   1.5 fov PARENT/scope_30mm_leupold_mark4_lr_6,5_20x50(Clone)
            /////9x    2.9 FOV parent/parent/scope_all_ncstar_advance_dual_optic_3_9x_42(Clone)
            /////8x    3   FOV parent/parent/scope_30mm_s&b_pm_ii_1_8x24(Clone)
            //////5x    3.6 FOV parent/parent/scope_34mm_s&b_pm_ii_5_25x56(Clone)
            ////////25x   1.9 FOV parent/parent/scope_34mm_s&b_pm_ii_5_25x56(Clone)



            //////4x    6  FoV parent/scope_25_4mm_vomz_pilad_4x32m(Clone) parent/scope_all_leupold_mark4_hamr(Clone) parent/scope_all_sig_bravo4_4x30(Clone)
            /////12x   1.9 FoV parent/parent/scope_34mm_s&b_pm_ii_3_12x50(Clone)
            ///////7x    1.6 FoV parent/parent/scope_34mm_nightforce_atacr_7_35x56(Clone)


            SightModVisualControllers visualController = opticSight.GetComponent<SightModVisualControllers>();
            if (!visualController)
                visualController = opticSight.transform.parent.GetComponent<SightModVisualControllers>();
            float fov = 27;
            if (visualController && vrOpticController) {
                scopeSensitivity = visualController.sightComponent_0.GetCurrentSensitivity;
                if (vrOpticController.scopeCamera == __instance.camera_0)
                {
                    fov = vrOpticController.currentFov;
                }
                else {
                    vrOpticController.scopeCamera = __instance.camera_0;
                    float zoomLevel = visualController.sightComponent_0.GetCurrentOpticZoom();
                    string scopeName = opticSight.name;
                    // For scopes that have multiple levels of zoom of different zoom effects (e.g. changing sight lines from black to red), opticSight will be stored in 
                    // mode_000, mode_001, etc, and that will be stored in the scope game object, so we need to get parent name for scopes with multiple settings
                    BoxCollider scopeCollider;
                    if (scopeName.Contains("mode_"))
                    {
                        scopeName = opticSight.transform.parent.name;
                        opticSight.transform.parent.gameObject.layer = 6;
                        scopeCollider = opticSight.transform.parent.GetComponent<BoxCollider>();
                    }
                    else { 
                        opticSight.gameObject.layer = 6;
                        scopeCollider = opticSight.GetComponent<BoxCollider>();
                    }
                    if (scopeCollider) {
                        scopeCollider.size = new Vector3(0.01f, 0.04f, 0.02f);
                        scopeCollider.center = new Vector3(-0.04f,0, -0.075f);
                        scopeCollider.enabled = true;
                    }
                    fov = ScopeManager.GetFOV(scopeName, zoomLevel);
                    vrOpticController.minFov = ScopeManager.GetMinFOV(scopeName);
                    vrOpticController.maxFov = ScopeManager.GetMaxFOV(scopeName);
                    vrOpticController.currentFov = fov;
                }

                if (opticSight.name.Contains("mode_"))
                    opticSight.transform.parent.GetComponent<BoxCollider>().enabled = true;
                else
                    opticSight.GetComponent<BoxCollider>().enabled = true;

                

            }
            __instance.camera_0.fieldOfView = fov;




                // The SightModeVisualControllers on the scopes contains sightComponent_0 which has a function GetCurrentOpticZoom which returns the zoom

            }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameUI), "Awake")]
        private static void w(GameUI __instance)
        {
            __instance.transform.parent = camRoot.transform;
            __instance.transform.localPosition = Vector3.zero;
            __instance.transform.localRotation = Quaternion.identity;

        }






        // SSAA causes a bunch of issues like thermal/nightvision rendering all fucky, and the scopes also render in 
        // with 2 other lenses on either side of the main lense, Although SSAA is nice for fixing the jagged edges, it 
        // also adds a strong layer of blur over everything so it's definitely best to keep it disabled. Might look into
        // keeping it around later on if I can figure a way to get it to look nice without messing with everything else
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SSAAPropagator), "Init")]
        private static void DisableSSAA(SSAAPropagator __instance)
        {
            Plugin.MyLog.LogWarning("SSAA Init\n");
            __instance._postProcessLayer = null;

        }

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(PrismEffects), "OnRenderImage")]
        //private static bool DisablePrismEffects(PrismEffects __instance)
        //{
        //    if (__instance.gameObject.name != "FPS Camera")
        //        return true;

        //    __instance.enabled = false;
        //    return false;
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(BloomAndFlares), "OnRenderImage")]
        //private static bool DisableBloomAndFlares(BloomAndFlares __instance)
        //{
        //    if (__instance.gameObject.name != "FPS Camera")
        //        return true;

        //    __instance.enabled = false;
        //    return false;
        //}


        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(ChromaticAberration), "OnRenderImage")]
        //private static bool DisableChromaticAberration(ChromaticAberration __instance)
        //{
        //    if (__instance.gameObject.name != "FPS Camera")
        //        return true;

        //    __instance.enabled = false;
        //    return false;
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(UltimateBloom), "OnRenderImage")]
        //private static bool DisableUltimateBloom(UltimateBloom __instance)
        //{
        //    if (__instance.gameObject.name != "FPS Camera")
        //        return true;

        //    __instance.enabled = false;
        //    return false;
        //}

        private static Camera postProcessingStoogeCamera; 
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PostProcessLayer), "InitLegacy")]
        private static void FixPostProcessing(PostProcessLayer __instance) { 
            if (camHolder && camHolder.GetComponent<Camera>() == null)
            {
                postProcessingStoogeCamera = camHolder.AddComponent<Camera>();
                postProcessingStoogeCamera.enabled = false;
            }
            if (postProcessingStoogeCamera)
                __instance.m_Camera = postProcessingStoogeCamera;
        }



        [HarmonyPostfix]
        [HarmonyPatch(typeof(BloodOnScreen), "Start")]
        private static void SetMainCamParent(BloodOnScreen __instance)
        {
            Camera mainCam = __instance.GetComponent<Camera>();
            if (mainCam.name == "FPS Camera") {
                Plugin.MyLog.LogWarning("\n\nSetting camera \n\n");
                mainCam.transform.parent = vrOffsetter.transform;
                mainCam.cullingMask = -1;
                mainCam.gameObject.AddComponent<SteamVR_TrackedObject>();
                //mainCam.gameObject.GetComponent<PostProcessLayer>().enabled = false;
                //cameraManager.initPos = VRCam.transform.localPosition;
            }

        }



        // Collimators try to do some stupid shit which stops them from displaying so disable it here
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CameraClass), "method_12")]
        private static bool FixCollimatorSights(CameraClass __instance)
        {
            return false;
        }

      
        // These two functions would return the screen resolution setting and would result in the game
        // being very blurry
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SSAAImpl), "GetOutputWidth")]
        private static bool ReturnVROutputWidth(SSAAImpl __instance, ref int __result)
        {
            __result = (int)Mathf.Ceil(__instance.GetInputWidth() * 1.25f);
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SSAAImpl), "GetOutputHeight")]
        private static bool ReturnVROutputHeight(SSAAImpl __instance, ref int __result)
        {
            __result = (int) Mathf.Ceil(__instance.GetInputHeight() * 1.25f);
            return false;
        }

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(SSAAImpl), "Awake")]
        //private static bool RemoveBadCameraEffects(SSAAImpl __instance)
        //{
        //    // All the SSAA stuff makes everything very blurry and bad quality, all while lowering framerate
        //    if (__instance.GetComponent<SSAAPropagatorOpaque>() != null)
        //    {
        //        UnityEngine.Object.Destroy(__instance.GetComponent<SSAAPropagatorOpaque>());
        //        Plugin.MyLog.LogWarning("SSAAPropagatorOpaque component removed successfully.");
        //    }
        //    if (__instance.GetComponent<SSAAPropagator>() != null)
        //    {
        //        UnityEngine.Object.Destroy(__instance.GetComponent<SSAAPropagator>());
        //        Plugin.MyLog.LogWarning("SSAAPropagator component removed successfully.");
        //    }
        //    if (__instance != null)
        //    {
        //        UnityEngine.Object.Destroy(__instance);
        //        Plugin.MyLog.LogWarning("SSAAImpl component removed successfully.");
        //    }
        //    if (__instance.GetComponent<SSAA>() != null)
        //    {
        //        UnityEngine.Object.Destroy(__instance.GetComponent<SSAA>());
        //        Plugin.MyLog.LogWarning("SSAA component removed successfully.");
        //    }
        //    // The EnableDistantShadowKeywords is responsible for rendering distant shadows (who woulda thunk) but it works
        //    // poorly with VR so it needs to be removed and should ideally be suplemented with high or ultra shadow settings
        //    CommandBuffer[] commandBuffers = Camera.main.GetCommandBuffers(CameraEvent.BeforeGBuffer);
        //    for (int i = 0; i < commandBuffers.Length; i++)
        //    {
        //        if (commandBuffers[i].name == "EnableDistantShadowKeywords")
        //            Camera.main.RemoveCommandBuffer(CameraEvent.BeforeGBuffer, commandBuffers[i]);
        //    }

        //    return false;
        //}


        //NOTEEEEEEEEEEEE: Removing the SSAApropagator (i think) from the PostProcessingLayer/Volume will restore some visual fidelity but still not as good as no ssaa

        //ANOTHER NOTE: I'm pretty sure if you delete or disable the SSAA shit you still get all the nice visual effects from the post processing without the blur,
        // its just the night vision doessn't work, so maybe only enable SSAA when enabling night/thermal vision

        // FIGURED IT OUT Delete the SSAAPropagator, SSAA, and SSAAImpl and it just works

        // Also remove the distant shadows command buffer from the camera
        // MotionVectorsPASS is whats causing the annoying [Error  : Unity Log] Dimensions of color surface does not match dimensions of depth surface    error to occur 
        // but its also needed for grass and maybe other stuff

        //VolumetricLightRenderer is whats rendering the lights in different eyes, look into that

        // Not sure if needed
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CameraClass), "method_10")]
        private static bool FixSomeAimShit(CameraClass __instance)
        {
            __instance.ReflexController.RefreshReflexCmdBuffers();
            Renderer renderer = __instance.OpticCameraManager.CurrentOpticSight?.LensRenderer;
            __instance.method_11();
            if (__instance.OpticCameraManager.CurrentOpticSight != null)
            {
                //LODGroup[] componentsInChildren = __instance.OpticCameraManager.CurrentOpticSight.gameObject.GetComponentInParent<WeaponPrefab>().gameObject.GetComponentsInChildren<LODGroup>();
               
                ////////////// Since the weapons are moved around to the right controller object, this needs to be redone here
                LODGroup[] componentsInChildren = oldWeaponHolder.GetComponentsInChildren<LODGroup>();
                if (__instance.renderer_0 != null)
                {
                    Array.Clear(__instance.renderer_0, 0, __instance.renderer_0.Length);
                }
                int instanceID = renderer.GetInstanceID();
                int num = 0;
                int num2 = 0;
                while (componentsInChildren != null && num2 < componentsInChildren.Length)
                {
                    if (!(componentsInChildren[num2] == null))
                    {
                        LOD[] lODs = componentsInChildren[num2].GetLODs();
                        if (lODs.Length != 0)
                        {
                            Renderer[] renderers = lODs[0].renderers;
                            foreach (Renderer renderer2 in renderers)
                            {
                                if (!(renderer2 == null) && renderer2.GetInstanceID() != instanceID && !__instance.method_9(renderer2.GetInstanceID()))
                                {
                                    num++;
                                }
                            }
                        }
                    }
                    num2++;
                }
                if (num > 0 && (__instance.renderer_0 == null || __instance.renderer_0.Length < num))
                {
                    __instance.renderer_0 = new Renderer[num];
                }
                num = 0;
                int num3 = 0;
                while (componentsInChildren != null && num3 < componentsInChildren.Length)
                {
                    if (!(componentsInChildren[num3] == null))
                    {
                        LOD[] lODs2 = componentsInChildren[num3].GetLODs();
                        if (lODs2.Length != 0)
                        {
                            Renderer[] renderers = lODs2[0].renderers;
                            foreach (Renderer renderer3 in renderers)
                            {
                                if (!(renderer3 == null) && renderer3.GetInstanceID() != instanceID && !__instance.method_9(renderer3.GetInstanceID()))
                                {
                                    __instance.renderer_0[num++] = renderer3;
                                }
                            }
                        }
                    }
                    num3++;
                }
            }
            __instance.SSAA.SetLensRenderer(__instance.OpticCameraManager.CurrentOpticSight?.LensRenderer, __instance.renderer_1, __instance.renderer_0);
            __instance.SSAA.UnityTAAJitterSamplesRepeatCount = 2;
            return false;
        }




        // GClass1913 is a class used by the PlayerCameraController to position and rotate the camera, PlayerCameraController holds the abstract class GClass1943 which this inherits
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GClass1916), "ManualLateUpdate")]
        private static bool StopCamXRotation(GClass1916 __instance)
        {
            //Plugin.MyLog.LogError(inGame + "   |   " + Time.deltaTime.ToString());
            if (__instance.player_0.IsAI || !inGame)
                return true;

            if (emptyHands)
                camRoot.transform.position = emptyHands.position;
            else
                camRoot.transform.position = __instance.method_1(camRoot.transform.position, camRoot.transform.rotation, __instance.transform_0.position);
            camRoot.transform.position = new Vector3(camRoot.transform.position.x, __instance.player_0.Transform.position.y + 1.5f, camRoot.transform.position.z);
            //camHolder.transform.position = __instance.transform_0.position + new Vector3(Test.ex, Test.ey, Test.ez);
            return false;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "SetCompensationScale")]
        private static void SetBodyIKScale(Player __instance)
        {
            if (__instance.IsAI)
                return;
            // If this isn't set to 1, then the hands start to stretch or squish when rotating them around
            __instance.RibcageScaleCurrentTarget = 1f;
            __instance.RibcageScaleCurrent = 1f;
        }


        private static float lastYRot = 0f;
        private static float timeSinceLastLookRot = 0f;
        // Found in Player object under CurrentState variable, and is inherited by Gclass1573
        // Can access MovementContext and probably state through player object->HideoutPlayer->MovementContext
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MovementState), "Rotate")]
        private static bool SetPlayerRotate(MovementState __instance, ref Vector2 deltaRotation)
        {
            if (__instance.MovementContext.IsAI)
                return true;

            if (menuOpen || !inGame)
                return false;

            bool leftJoystickUsed = (Mathf.Abs(SteamVR_Actions._default.LeftJoystick.axis.x) > MIN_JOYSTICK_AXIS_FOR_MOVEMENT || Mathf.Abs(SteamVR_Actions._default.LeftJoystick.axis.y) > MIN_JOYSTICK_AXIS_FOR_MOVEMENT);
            bool leftJoystickLastUsed = (Mathf.Abs(SteamVR_Actions._default.LeftJoystick.lastAxis.x) > MIN_JOYSTICK_AXIS_FOR_MOVEMENT || Mathf.Abs(SteamVR_Actions._default.LeftJoystick.lastAxis.y) > MIN_JOYSTICK_AXIS_FOR_MOVEMENT);
            
            // Normally you'd stand with your left foot forward and right foot back, which doesn't feel natural in VR so rotate 28 degrees to have both feet in front when standing still
            Vector3 bodyForward = Quaternion.Euler(0, 28, 0) * __instance.MovementContext._player.gameObject.transform.forward;
            Vector3 cameraForward = Camera.main.transform.forward;
            float rotDiff = Vector3.SignedAngle(bodyForward, cameraForward, Vector3.up);


            if (leftJoystickUsed)
            {
                lastYRot = Camera.main.transform.eulerAngles.y;
            }
            else if (SteamVR_Actions._default.RightJoystick.axis.x != 0)
            {
                lastYRot = camRoot.transform.eulerAngles.y;
            }
            else if (Mathf.Abs(rotDiff) > 80 && timeSinceLastLookRot > 0.25)
            {
                lastYRot += rotDiff;
                timeSinceLastLookRot = 0;
            }
            timeSinceLastLookRot += Time.deltaTime;
            if (!leftJoystickUsed && leftJoystickLastUsed)
                lastYRot -= 40;
            deltaRotation = new Vector2(deltaRotation.x + lastYRot, 0);

          
            // If difference between cam and body exceed something when using the right joystick, then turn the body.
            // Keep it a very tight amount before the body starts to rotate since the arms will become fucky otherwise

            //------- MovementContext has some interesting variables like trunk rotation limit, worth looking at maybe

            // ------- When body 45 degrees in either direction thats when it moves and it goes to the other extreme, e.g. exceeding 45 goes to 315

            //camRoot.transform.Rotate(0, __instance.MovementContext.Rotation.x - deltaRotation.x,0);

            __instance.MovementContext.Rotation = deltaRotation;
            //__instance.MovementContext._player.Transform.rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y-40, 0);


            return false;
        }

        // GClass1854 inherits MovementState and its version of ProcessUpperbodyRotation is ran when the player is moving, this ensures 
        // the rotation when not moving is similar to that when moving otherwise the player can rotate their camera faster than the body
        // can rotate
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MovementState), "ProcessUpperbodyRotation")]
        private static bool FixBodyRotation(MovementState __instance, float deltaTime)
        {
            //float y = Mathf.Abs(__instance.MovementContext.TransformRotation.eulerAngles.y - camRoot.transform.eulerAngles.y);
            //if (y > 20)
            //    __instance.MovementContext.ApplyRotation(Quaternion.Lerp(__instance.MovementContext.TransformRotation, __instance.MovementContext.TransformRotation * Quaternion.Euler(0f, y, 0f), 30f * deltaTime));
            if (__instance.MovementContext._player.IsAI || inGame) 
                return true;

            __instance.UpdateRotationSpeed(deltaTime);
            float float_3 = __instance.MovementContext.TransformRotation.eulerAngles.y;
            float f = Mathf.DeltaAngle(__instance.MovementContext.Yaw, float_3);
            float num = Mathf.InverseLerp(10f, 45f, Mathf.Abs(f)) + 1f;
            float_3 = Mathf.LerpAngle(float_3, __instance.MovementContext.Yaw, EFTHardSettings.Instance.TRANSFORM_ROTATION_LERP_SPEED * deltaTime * num);
            __instance.MovementContext.ApplyRotation(Quaternion.AngleAxis(float_3, Vector3.up) * __instance.MovementContext.AnimatorDeltaRotation);
            //Plugin.MyLog.LogError("Process upper " + (Quaternion.AngleAxis(float_3, Vector3.up) * __instance.MovementContext.AnimatorDeltaRotation).eulerAngles);
            return false;
        }




        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "UpdateBonesOnWeaponChange")]
        private static void FixLeftArmBendGoal(Player __instance)
        {
            if (__instance.IsAI)
                return;
            // Change the elbow bend from the weapons left arm goal to the player bodies bend goal, otherwise the left arms bend goal acts like its
            // still attached to the gun even when its not
            __instance._elbowBends[0] = __instance.PlayerBones.BendGoals[0];
        }




        [HarmonyPrefix]
        [HarmonyPatch(typeof(HideoutPlayer), "SetPatrol")]
        private static bool PreventGunBlockInHideout(HideoutPlayer __instance, ref bool patrol)
        {
            if (oldWeaponHolder != null)
                patrol = false;

            return true;
        }


    }
    

    // CROUCHING:
    // Gclass1603 class, found in Player object under gclass1603 variable float_2 is crouching value between 0f and 1f
    //                   Get and Set IsInPronePose for proning, seems to call on action_6 variable Invoke() method with PoseToInt as arg 0. Just need to change the IsInPronePose variable to set prone
                        
    // CHANGE WEAPON:
    // Player class, method smethod_7 takes in a ItemHandsController type as an argument and seems to swap weapons
}


// In hideout, don't notice any real fps difference when changing object LOD quality and overall visibility

// anti aliasing is off or on FXAA - no FPS difference noticed - seems like scopes won't work without it
// Resampling x1 OFF 
// DLSS and FSR OFF
// HBAO - Looks better but takes a massive hit on performance - off gets about around 10-20 fps increase
// SSR - turning low to off raises FPS by about 2-5, turning ultra to off raises fps by about 5ish. I don't know if it looks better but it seems like if you have it on, you may as well go to ultra
// Anistrophic filtering - per texture or on maybe cos it looks bettter, or just off - No real FPS difference
// Sharpness at 1-1.5 I think it the gain falls off after around 1.5+
// Uncheck all boxes on bottom - CHROMATIC ABBERATIONS probably causing scope issues so always have it off
// Uncheck all boxes on bottom - CHROMATIC ABBERATIONS probably causing scope issues so always have it off

