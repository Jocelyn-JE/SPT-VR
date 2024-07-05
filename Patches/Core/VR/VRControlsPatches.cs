﻿using EFT.InputSystem;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using TarkovVR.Source.Player.VRManager;
using TarkovVR.Patches.Core.Player;
using TarkovVR.Source.Controls;

namespace TarkovVR.Patches.Core.VR
{
    [HarmonyPatch]
    internal class VRControlsPatches
    {

        private static bool isScrolling;
        //private static bool isAiming = false;
        private static bool isHoldingBreath = false;
        private static bool isSprinting = false;
        private static bool isShooting = false;
        private static ECommand[] quickSlotCommands = { ECommand.SelectFastSlot4, ECommand.SelectFastSlot5, ECommand.SelectFastSlot6, ECommand.SelectFastSlot7, ECommand.SelectFastSlot8, ECommand.SelectFastSlot9, ECommand.SelectFastSlot0 };
        //------------------------------------------------------------------------------------------------------------------------------------------------------------


        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(GClass1765), "UpdateBindings")]
        //private static bool BlockLookAwxis(GClass1765 __instance, KeyGroup[] keyGroups, AxisGroup[] axisGroups, float doubleClickTimeout)
        //{
        //    for (int i = 0; i < keyGroups.Length; i++) {
        //        Plugin.MyLog.LogWarning(i + ": " + keyGroups[i].keyName + "\n");

        //    }
        //    return true;
        //}

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GClass1765), "UpdateInput")]
        private static bool MasterVRControls(GClass1765 __instance, ref List<ECommand> commands, ref float[] axis, ref float deltaTime)
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
            bool interactMenuOpen = (VRGlobals.vrPlayer && VRGlobals.vrPlayer.interactionUi && VRGlobals.vrPlayer.interactionUi.gameObject.active);
            if (VRGlobals.firearmController)
                isAiming = VRGlobals.firearmController.IsAiming;


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

                VRInputManager.UpdateCommands(ref commands);

                //for (int k = 0; k < __instance.gclass1760_0.Length; k++)
                //{
                //    __instance.ecommand_0 = __instance.gclass1760_0[k].UpdateCommand(deltaTime);
                //    if (VRGlobals.inGame && !VRGlobals.menuOpen)
                //    {
                        

                //    }
                //    if (k == ((int)ECommand.Escape) && SteamVR_Actions._default.ButtonB.GetStateDown(SteamVR_Input_Sources.Any) && !WeaponPatches.returnAfterGrenade) { 
                //        if (VRGlobals.usingItem)
                //            __instance.ecommand_0 = EFT.InputSystem.ECommand.ToggleShooting;
                //        else
                //            __instance.ecommand_0 = EFT.InputSystem.ECommand.Escape;
                //    }
                //    if (__instance.ecommand_0 != 0)
                //    {
                //        commands.Add(__instance.ecommand_0);
                //        //Plugin.MyLog.LogError(k + ": " + (__instance.gclass1760_0[k] as GClass1802).GameKey + "\n");
                //    }
                //    //if (__instance.gclass1760_0[k].GetInputCount() != 0)
                //    //    Plugin.MyLog.LogWarning(i + ": " + __instance.ginterface141_0 [i].GetValue() + "\n");
                //}
            }

            for (int l = 0; l < axis.Length; l++)
            {
                axis[l] = 0f;
            }

            if (__instance.gclass1761_1 == null)
            {
                return false;
            }
            if (VRGlobals.inGame && !VRGlobals.menuOpen )
            {
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
                        if (Mathf.Abs(SteamVR_Actions._default.RightJoystick.axis.y) < 0.75f && !VRGlobals.blockRightJoystick && Mathf.Abs(SteamVR_Actions._default.RightJoystick.axis.x) > 0.1f)
                            axis[__instance.gclass1761_1[m].IntAxis] = SteamVR_Actions._default.RightJoystick.axis.x * 5;
                        else
                            axis[__instance.gclass1761_1[m].IntAxis] = 0;
                        if (VRGlobals.camRoot != null)
                            VRGlobals.camRoot.transform.Rotate(0, axis[__instance.gclass1761_1[m].IntAxis], 0);
                    }
                    else if (m == 0 && Mathf.Abs(SteamVR_Actions._default.LeftJoystick.axis.x) > VRGlobals.MIN_JOYSTICK_AXIS_FOR_MOVEMENT)
                        axis[__instance.gclass1761_1[m].IntAxis] = SteamVR_Actions._default.LeftJoystick.axis.x;
                    else if (m == 1 && Mathf.Abs(SteamVR_Actions._default.LeftJoystick.axis.y) > VRGlobals.MIN_JOYSTICK_AXIS_FOR_MOVEMENT)
                        axis[__instance.gclass1761_1[m].IntAxis] = SteamVR_Actions._default.LeftJoystick.axis.y;

                    //else if (leftArmIk)
                    //{
                    //    Vector3 headsetPos = Camera.main.transform.position;
                    //    Vector3 playerBodyPos = leftArmIk.transform.root.position + VRPlayerManager.headOffset;
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
                //if (VRGlobals.inGame && VRGlobals.player)
                //{
                //    // Base Height - the height at which crouching begins.
                //    float baseHeight = VRGlobals.vrPlayer.initPos.y * 0.90f; // 90% of init height
                //                                                             // Floor Height - the height at which full prone is achieved.
                //    float floorHeight = VRGlobals.vrPlayer.initPos.y * 0.40f; // Significant crouch/prone

                //    // Current height position normalized between baseHeight and floorHeight.
                //    float normalizedHeightPosition = (Camera.main.transform.localPosition.y - floorHeight) / (baseHeight - floorHeight);

                //    // Ensure the normalized height is within 0 (full crouch/prone) and 1 (full stand).
                //    float crouchLevel = Mathf.Clamp(normalizedHeightPosition, 0, 1);



                //    // Handling prone based on crouchLevel instead of raw height differences.
                //    if (normalizedHeightPosition < -0.2 && VRGlobals.player.MovementContext.CanProne) // Example threshold for prone
                //        VRGlobals.player.MovementContext.IsInPronePose = true;
                //    else
                //        VRGlobals.player.MovementContext.IsInPronePose = false;

                //    // Debug or apply the crouch level
                //    //Plugin.MyLog.LogError("Crouch Level: " + crouchLevel + " " + normalizedHeightPosition);
                //    VRGlobals.player.MovementContext._poseLevel = crouchLevel;

                //    //player.MovementContext._poseLevel = crouchLevel;
                //}
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


    }
}


//0: LeanLockRight
//1: LeanLockLeft
    //2: Shoot
//3: Aim
    //4: ChangeAimScope
    //5: ChangeAimScopeMagnification
//6: Nidnod
    //7: ToggleGoggles
    //8: ToggleHeadLight
    //9: SwitchHeadLight
//10: ToggleVoip
//11: PushToTalk
//12: Mumble
//13: MumbleDropdown
//14: MumbleQuick
    //15: WatchTime
    //16: WatchTimerAndExits
    //17: Tactical
    //18: NextTacticalDevice
//19: Next
//20: Previous
//21: Interact
//22: ThrowGrenade
//23: ReloadWeapon
//24: QuickReloadWeapon
//25: DropBackpack
//26: NextMagazine
//27: PreviousMagazine
//28: ChangePointOfView
//29: CheckAmmo
//30: ShootingMode
//31: ForceAutoWeaponMode
//32: CheckFireMode
//33: CheckChamber
//34: ChamberUnload
//35: UnloadMagazine
//36: Prone
//37: Sprint
//38: Duck
//39: NextWalkPose
//40: PreviousWalkPose
//41: Walk
//42: BlindShootAbove
//43: BlindShootRight
//44: StepRight
//45: StepLeft
//46: ExamineWeapon
//47: FoldStock
//48: Inventory
//49: Jump
//50: Knife
//51: QuickKnife
//52: PrimaryWeaponFirst
//53: PrimaryWeaponSecond
//54: SecondaryWeapon
//55: QuickSecondaryWeapon
//56: Slot4
//57: Slot5
//58: Slot6
//59: Slot7
//60: Slot8
//61: Slot9
//62: Slot0
//63: OpticCalibrationSwitchUp 
//64: OpticCalibrationSwitchDown
//65: MakeScreenshot
//66: ThrowItem
//67: Breath
//68: ToggleInfo
//69: Console
//70: PressSlot4
//71: PressSlot5
//72: PressSlot6
//73: PressSlot7
//74: PressSlot8
//75: PressSlot9
//76: PressSlot0
//77: F1
//78: DoubleF1
//79: F2
//80: DoubleF2
//81: F3
//82: DoubleF3
//83: F4
//84: DoubleF4
//85: F5
//86: DoubleF5
//87: F6
//88: DoubleF6
//89: F7
//90: DoubleF7
//91: F8
//92: DoubleF8
//93: F9
//94: DoubleF9
//95: F10
//96: DoubleF10
//97: F11
//98: DoubleF11
//99: F12
//100: DoubleF12
//101: Enter
//102: Escape
//103: HighThrow
//104: LowThrow