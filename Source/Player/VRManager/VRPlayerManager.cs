﻿using EFT.InventoryLogic;
using Newtonsoft.Json.Linq;
using Sirenix.Serialization;
using System.Collections.Generic;
using TarkovVR;
using TarkovVR.Patches.Core.Player;
using TarkovVR.Patches.Core.VR;
using TarkovVR.Patches.UI;
using TarkovVR.Source.Controls;
using TarkovVR.Source.Misc;
using TarkovVR.Source.Settings;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using static RootMotion.FinalIK.FBIKChain;

namespace TarkovVR.Source.Player.VRManager
{
    internal abstract class VRPlayerManager : MonoBehaviour
    {

        public static Transform leftWrist;



        public Vector3 initPos;
        public Vector3 x;
        private Vector3 nonSupportRightHandRotOffset = new Vector3(0, 170, 50);
        public static Vector3 headOffset = new Vector3(0.04f, 0.175f, 0.07f);
        private Vector3 supportingLeftHandOffset = new Vector3(355, 15, 90);
        public Transform gunTransform;
        public static Transform leftHandGunIK;
        public bool leftHandOnScope = false;

        public float leftHandYRotation = 0f;
        public float leftHandZRotation = 0f;
        public static float smoothingFactor = 100f; // Adjust this value to lower to increase aim smoothing - 20 is barely noticable so good baseline

        // VR Origin and body stuff
        public GameObject LeftHand = null;
        public GameObject RightHand = null;

        public GameObject radialMenu;
        protected GameObject leftWristUi;


        public bool isSupporting = false;
        public bool wasSupporting = false;
        private float timeHeld = 0;
        public Transform interactionUi;
        public Vector3 startingPlace;
        private bool showingHealthUi = false;
        private bool showingExtractionUi = false;
        private bool handLock = false;
        public bool blockJump = true;
        public bool blockCrouch = true;
        public bool interactMenuOpen = false;
        private static int LEFT_HAND_ANIMATOR_HASH = UnityEngine.Animator.StringToHash("ReloadFloat");
        private Transform ammoFireModeUi;
        private bool isAmmoCount = false;
        //public VRInputManager inputManager;
        private bool leftHandInAnimation = false;
        public bool showScopeZoom = false;
        public float crouchHeightDiff = 0;
        public Transform scopeUiPosition;
        public bool isWeapPistol = false;
        public int framesAfterSwitching = 0;


        public void SetAmmoFireModeUi(Transform uiObject, bool isAmmoCount) {
            if (uiObject == null && ammoFireModeUi != null)
                ammoFireModeUi.position = Vector3.zero;
            this.isAmmoCount = isAmmoCount;
            ammoFireModeUi = uiObject;

        }

        private Vector3 currentRightHandVelocity;

        private Queue<Vector3> velocityHistory = new Queue<Vector3>();
        private int maxVelocitySamples = 5;
        private Vector3 lastRightHandPosition;

        private void TrackVelocity(Transform handTransform)
        {
            //Vector3 currentVelocity = (handTransform.localPosition - lastRightHandPosition) / Time.deltaTime;
            //lastRightHandPosition = handTransform.localPosition;

            //// Add the current velocity to the history
            //if (velocityHistory.Count >= maxVelocitySamples)
            //{
            //    velocityHistory.Dequeue(); // Remove the oldest velocity
            //}
            //velocityHistory.Enqueue(currentVelocity);

            // Convert the hand position to the local space of the VR rig
            Vector3 localPosition = VRGlobals.vrOffsetter.transform.InverseTransformPoint(handTransform.position);

            Vector3 currentVelocity = (localPosition - lastRightHandPosition) / Time.deltaTime;
            lastRightHandPosition = localPosition;

            // Add the current velocity to the history
            if (velocityHistory.Count >= maxVelocitySamples)
            {
                velocityHistory.Dequeue(); // Remove the oldest velocity
            }
            velocityHistory.Enqueue(currentVelocity);
        }
        private Vector3 CalculateAverageVelocity()
        {
            Vector3 sum = Vector3.zero;
            foreach (Vector3 velocity in velocityHistory)
            {
                sum += velocity;
            }
            return sum / velocityHistory.Count;
        }

        protected virtual void Awake()
        {
            SpawnHands();
            x.x = 0.075f;
            Plugin.MyLog.LogWarning("Create hands");
            if (RightHand) { 
                RightHand.transform.parent = VRGlobals.vrOffsetter.transform;
                if (!radialMenu)
                {
                    radialMenu = new GameObject("radialMenu");
                    radialMenu.layer = 5;
                    radialMenu.transform.parent = RightHand.transform;
                    CircularSegmentUI uiComp = radialMenu.AddComponent<CircularSegmentUI>();
                    uiComp.Init();
                    //uiComp.CreateGunUi(new string[] { "reload.png", "checkAmmo.png", "inspect.png", "fixMalfunction.png", "fireMode_burst.png" });
                    uiComp.CreateGunUi(new string[] { "firstPrimary.png", "secondPrimary.png", "pistol.png", "knife.png"});
                    radialMenu.active = false;
                }
            }
            if (LeftHand) { 
                LeftHand.transform.parent = VRGlobals.vrOffsetter.transform;
                if (!leftWristUi) {
                    leftWristUi = new GameObject("leftWristUi");
                    leftWristUi.layer = 5;
                    leftWristUi.transform.parent = LeftHand.transform;
                    leftWristUi.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                    //leftWristUi.transform.localPosition = new Vector3(0, -0.05f, 0.015f);
                    //leftWristUi.transform.localEulerAngles = Vector3.zero;

                    //UIPatches.healthPanel.transform.parent = leftWristUi.transform;
                    //UIPatches.healthPanel.transform.localPosition = Vector3.zero;
                    //UIPatches.healthPanel.transform.localEulerAngles = new Vector3(270,87,0);

                    //UIPatches.stancePanel.transform.parent = leftWristUi.transform;
                    //UIPatches.stancePanel.transform.localPosition = new Vector3(0.1f, 0, 0.03f);
                    //UIPatches.stancePanel.transform.localEulerAngles = new Vector3(270, 87, 0);

                }
            }
            SteamVR_Actions._default.RightHandPose.RemoveAllListeners(SteamVR_Input_Sources.RightHand);
            SteamVR_Actions._default.LeftHandPose.RemoveAllListeners(SteamVR_Input_Sources.LeftHand);

            SteamVR_Actions._default.RightHandPose.AddOnUpdateListener(SteamVR_Input_Sources.RightHand, UpdateRightHand);
            SteamVR_Actions._default.LeftHandPose.AddOnUpdateListener(SteamVR_Input_Sources.LeftHand, UpdateLeftHand);

            //if (inputManager == null) {
            //    inputManager = new VRInputManager();
            //}

        }

        public abstract void PositionLeftWristUi();
        public void SetNotificationUi()
        {
            if (UIPatches.notifierUi)
            {
                UIPatches.notifierUi.transform.parent = leftWristUi.transform;
                UIPatches.notifierUi.transform.localPosition = new Vector3(0.12f, 0f, -0.085f);
                UIPatches.notifierUi.transform.localEulerAngles = new Vector3(272, 163, 283);
                UIPatches.notifierUi.transform.localScale = new Vector3(0.0003f, 0.0003f, 0.0003f);
            }
        }

        public void OnDisable()
        {
            SteamVR_Actions._default.RightHandPose.RemoveOnUpdateListener(SteamVR_Input_Sources.RightHand, UpdateRightHand);
            SteamVR_Actions._default.LeftHandPose.RemoveOnUpdateListener(SteamVR_Input_Sources.LeftHand, UpdateLeftHand);

        }
        public void OnEnable()
        {
            SteamVR_Actions._default.RightHandPose.AddOnUpdateListener(SteamVR_Input_Sources.RightHand, UpdateRightHand);
            SteamVR_Actions._default.LeftHandPose.AddOnUpdateListener(SteamVR_Input_Sources.LeftHand, UpdateLeftHand);
        }


        private static Transform lastHitGunComp;
        protected virtual void Update()
        {
            if (Camera.main == null)
                return;
            if (initPos.y == 0)
                initPos = Camera.main.transform.localPosition;

            Vector3 newLocalPos = initPos * -1 + headOffset;
            newLocalPos.y -= crouchHeightDiff;
            VRGlobals.vrOffsetter.transform.localPosition = newLocalPos;

            interactMenuOpen = (interactionUi && interactionUi.GetChild(3) && interactionUi.GetChild(3).gameObject.active);
            blockJump = VRGlobals.blockRightJoystick || VRGlobals.menuOpen || interactMenuOpen || crouchHeightDiff != 0 || (VRGlobals.firearmController && VRGlobals.firearmController.IsAiming && SteamVR_Actions._default.RightGrip.state);
            blockCrouch = VRGlobals.blockRightJoystick || VRGlobals.menuOpen || interactMenuOpen || (VRGlobals.firearmController && VRGlobals.firearmController.IsAiming && SteamVR_Actions._default.RightGrip.state);


            // For Ammo do the exact same vu
            if (ammoFireModeUi != null)
            {
                if (isAmmoCount)
                {
                    ammoFireModeUi.rotation = WeaponPatches.currentGunInteractController.magazine.rotation;
                    ammoFireModeUi.Rotate(0, 90, 90);
                    ammoFireModeUi.position = WeaponPatches.currentGunInteractController.magazine.position;
                    //ammoFireModeUi.localPosition += x;
                    ammoFireModeUi.position += (ammoFireModeUi.right * 0.03f) + (ammoFireModeUi.forward * -0.0175f);
                }
                else {
                    ammoFireModeUi.rotation = WeaponPatches.currentGunInteractController.GetFireModeSwitch().rotation;
                    ammoFireModeUi.Rotate(0, 90, 90);
                    ammoFireModeUi.position = WeaponPatches.currentGunInteractController.GetFireModeSwitch().position;
                    ammoFireModeUi.position += (ammoFireModeUi.right* 0.03f) + (ammoFireModeUi.forward* -0.0175f);

                }
            }

            if (showScopeZoom && UIPatches.opticUi && scopeUiPosition) {
                UIPatches.opticUi.transform.rotation = scopeUiPosition.rotation;
                UIPatches.opticUi.transform.Rotate(90,0,0);
                UIPatches.opticUi.transform.position = scopeUiPosition.position;
                UIPatches.opticUi.transform.position += (scopeUiPosition.right * 0.05f) + (scopeUiPosition.forward * -0.01f);
            }




            // NOTESSSS: Append whatever physical crouch value you get thats between 0 and 1 and add it to (1 - (VRGlobals.vrPlayer.crouchHeightDiff / 0.4)) 
            // then clamp it to 0 and 1

            if (VRGlobals.player)
            {
                // Base Height - the height at which crouching begins.
                float baseHeight = initPos.y * 0.90f; // 90% of init height
                                                                         // Floor Height - the height at which full prone is achieved.
                float floorHeight = initPos.y * 0.50f; // Significant crouch/prone

                // Current height position normalized between baseHeight and floorHeight.
                float normalizedHeightPosition = (Camera.main.transform.localPosition.y - floorHeight) / (baseHeight - floorHeight);

                // Ensure the normalized height is within 0 (full crouch/prone) and 1 (full stand).
                float crouchLevel = 1 - Mathf.Clamp(normalizedHeightPosition, 0, 1);
                
                // crouchHeightDiff at max will be 0.4 when the joystick is used to crouch which will return a value between 0 and 1 which when subtracted from 1 
                // will return a value that can be used to subtract the physical crouch value from and will combine the physical and joystick crouching
                crouchLevel = Mathf.Clamp((1 - crouchHeightDiff / 0.4f) - crouchLevel,0,1);

                VRGlobals.player.MovementContext._poseLevel = crouchLevel;

                // Handling prone based on crouchLevel instead of raw height differences.
                //if (normalizedHeightPosition < -0.2 && VRGlobals.player.MovementContext.CanProne) // Example threshold for prone
                //    VRGlobals.player.MovementContext.IsInPronePose = true;
                //else
                //    VRGlobals.player.MovementContext.IsInPronePose = false;

                // Debug or apply the crouch level
            //Plugin.MyLog.LogError("Crouch Level:  " + crouchLevel  + "   |   " + VRGlobals.player.PoseLevel + "   |   " + ( 1 - VRGlobals.vrPlayer.crouchHeightDiff / 0.4));
                //Plugin.MyLog.LogError("Crouch Level: " + crouchLevel + "   | " + normalizedHeightPosition + "  |   " + VRGlobals.player.PoseLevel);
                //VRGlobals.player.ChangePose(-1.5f * Time.deltaTime);

            }


        }
        // localpos 0.12 0 -0.085
        // Rot 272.0235 163.5639 283.3635
        // scale 0.0003 0.0003 0.0003
        private float controllerLength = 0.175f;
        private Quaternion initialRightHandRotation;
        private Quaternion rotDiff;
        private bool isEnteringTwoHandedMode = false;
        public Transform rawRightHand;

        private Vector3 inertiaVelocity;
        private float inertiaDamping = 0.95f;  // Controls how quickly inertia fades
        private float returnSpeed = 5.0f;     // Controls how quickly the gun returns to the hand

        private void ApplyInertia(Transform handTransform, Transform targetHandTransform, Vector3 currentVelocity)
        {
            // Calculate the average velocity from the history
            Vector3 averageVelocity = CalculateAverageVelocity();

            // Trigger inertia when the current frame's velocity drops below the threshold
            if (currentVelocity.magnitude < 0.1f && averageVelocity.magnitude > 0.2f && averageVelocity.magnitude > inertiaVelocity.magnitude)
            {
                inertiaVelocity = averageVelocity;
            }

            // Apply inertia if it's active
            if (inertiaVelocity.magnitude > 0.01f && VRGlobals.firearmController)
            {

                // Apply inertia effect by moving the hand in the direction of inertia velocity in local space
                Vector3 localPosition = VRGlobals.vrOffsetter.transform.InverseTransformPoint(rawRightHand.position);
                localPosition += inertiaVelocity * (x.x * VRGlobals.firearmController.ErgonomicWeight);

                // Convert back to world space and apply the adjusted position
                rawRightHand.position = VRGlobals.vrOffsetter.transform.TransformPoint(localPosition);

                // Dampen the inertia over time
                inertiaVelocity *= inertiaDamping;
            }

        }

        private Vector3 preVelocityRightHandPos;
        private void UpdateRightHand(SteamVR_Action_Pose fromAction, SteamVR_Input_Sources fromSource)
        {
            if (!RightHand)
                return;


            if (VRGlobals.blockRightJoystick == true && !SteamVR_Actions._default.RightGrip.GetState(SteamVR_Input_Sources.RightHand))
            {
                Vector2 joystickInput = SteamVR_Actions._default.RightJoystick.axis;
                if (Mathf.Abs(joystickInput.x) < 0.2f && Mathf.Abs(joystickInput.y) < 0.2)
                    VRGlobals.blockRightJoystick = false;
            }

            if (VRGlobals.firearmController && isSupporting && !isWeapPistol)
            {

                if (VRGlobals.firearmController.IsAiming && VRGlobals.vrOpticController && SteamVR_Actions._default.RightGrip.state)
                {
                    VRGlobals.vrOpticController.handleJoystickZoomDial();
                    VRGlobals.blockRightJoystick = true;
                }
                else
                    VRGlobals.blockRightJoystick = false;


                Quaternion combinedRotation = Quaternion.LookRotation((LeftHand.transform.position - RightHand.transform.position).normalized, RightHand.transform.up);

                if (!isEnteringTwoHandedMode)
                {
                    framesAfterSwitching = 0;
                    VRGlobals.weaponHolder.transform.localPosition = new Vector3(-0.1927f, -0.1642f, -0.2195f);
                    VRGlobals.weaponHolder.transform.localRotation = Quaternion.Euler(355, 15, 85);
                    VRGlobals.player._markers[0] = WeaponPatches.previousLeftHandMarker;
                    // Capture the initial rotation when entering two-handed mode
                    rotDiff = initialRightHandRotation * Quaternion.Inverse(combinedRotation);
                    rotDiff = Quaternion.Euler(rotDiff.x, rotDiff.y, 0);
                    isEnteringTwoHandedMode = true;
                    // when changing from one to two handing, the rawRightHand rotation is off so when slerping it, the gun always starts
                    // pointing down, so for the first frame, instantly set its rotation before slerping
                    rawRightHand.transform.rotation = Quaternion.Euler(combinedRotation.eulerAngles + rotDiff.eulerAngles);

                }

                combinedRotation = Quaternion.Euler(combinedRotation.eulerAngles + rotDiff.eulerAngles);

                if (smoothingFactor < 50)
                {
                    if (VRSettings.SmoothScopeAim())
                        rawRightHand.transform.rotation = Quaternion.Slerp(rawRightHand.transform.rotation, combinedRotation, smoothingFactor * Time.deltaTime);
                    else
                        rawRightHand.transform.rotation = combinedRotation;
                }
                else if (VRSettings.SmoothWeaponAim() || VRSettings.GetWeaponWeightOn())
                {
                    float smoothing = smoothingFactor;
                    if (VRSettings.SmoothWeaponAim())
                        smoothing = VRSettings.GetSmoothingSensitivity();
                    if (VRSettings.GetWeaponWeightOn())
                        smoothing = smoothingFactor / VRGlobals.firearmController.ErgonomicWeight;
                    rawRightHand.transform.rotation = Quaternion.Slerp(rawRightHand.transform.rotation, combinedRotation, smoothing * Time.deltaTime);
                }
                else
                    rawRightHand.transform.rotation = combinedRotation;


                RightHand.transform.localRotation = fromAction.localRotation;
                RightHand.transform.Rotate(VRSettings.GetWeaponAngleOffset(), nonSupportRightHandRotOffset.y, nonSupportRightHandRotOffset.z + VRSettings.GetRightHandHorizontalOffset());
                Vector3 virtualBasePosition = fromAction.localPosition - fromAction.localRotation * Vector3.forward * controllerLength;
                RightHand.transform.localPosition = virtualBasePosition;

                if (VRSettings.SmoothWeaponAim() || VRSettings.GetWeaponWeightOn())
                {
                    float smoothing = 50;
                    if (VRSettings.SmoothWeaponAim())
                        smoothing = VRSettings.GetSmoothingSensitivity();
                    if (VRSettings.GetWeaponWeightOn())
                        smoothing = smoothingFactor / VRGlobals.firearmController.ErgonomicWeight;

                    Vector3 pos = rawRightHand.transform.position - (inertiaVelocity / inertiaDamping);

                    rawRightHand.transform.position = Vector3.Slerp(pos, RightHand.transform.position, smoothing * Time.deltaTime);

                }
                else
                    rawRightHand.transform.position = RightHand.transform.position;

                //Plugin.MyLog.LogWarning(RightHand.transform.rotation.eulerAngles + "     |   " + rawRightHand.transform.rotation.eulerAngles + "  |  " + VRGlobals.weaponHolder.transform.rotation.eulerAngles);

                if (framesAfterSwitching < 2) { 
                    VRGlobals.weaponHolder.transform.parent.position = rawRightHand.transform.position;
                    VRGlobals.weaponHolder.transform.parent.rotation = rawRightHand.transform.rotation;
                    framesAfterSwitching++;
                }
            }
            else
            {
                if (isEnteringTwoHandedMode)
                {
                    framesAfterSwitching = 0;
                    isEnteringTwoHandedMode = false;
                    VRGlobals.player._markers[0] = LeftHand.transform;
                    //VRGlobals.ikManager.leftArmIk.solver.target = LeftHand.transform;
                    VRGlobals.weaponHolder.transform.localPosition = WeaponPatches.weaponOffset;
                    VRGlobals.weaponHolder.transform.localRotation = Quaternion.Euler(15, 275, 90);
                    VRGlobals.firearmController.WeaponRoot.localPosition = new Vector3(0.1327f, -0.0578f, -0.0105f);
                    rawRightHand.transform.rotation = RightHand.transform.rotation;
                }

                RightHand.transform.localRotation = fromAction.localRotation;
                RightHand.transform.Rotate(VRSettings.GetWeaponAngleOffset(), nonSupportRightHandRotOffset.y, nonSupportRightHandRotOffset.z + VRSettings.GetRightHandHorizontalOffset());

                Vector3 virtualBasePosition = fromAction.localPosition - fromAction.localRotation * Vector3.forward * controllerLength;
                RightHand.transform.localPosition = virtualBasePosition;

                // Smoothing if weight is on
                if (VRGlobals.firearmController && VRSettings.GetWeaponWeightOn())
                {
                    float smoothing = smoothingFactor;
                    if (VRSettings.SmoothWeaponAim())
                        smoothing = VRSettings.GetSmoothingSensitivity();
                    if (VRSettings.GetWeaponWeightOn())
                        smoothing = smoothingFactor / VRGlobals.firearmController.ErgonomicWeight;
                    rawRightHand.transform.rotation = Quaternion.Slerp(rawRightHand.transform.rotation, RightHand.transform.rotation, smoothing * Time.deltaTime);
                }
                else
                    rawRightHand.transform.rotation = RightHand.transform.rotation;

                if (VRGlobals.firearmController && VRSettings.GetWeaponWeightOn())
                {
                    float smoothing = smoothingFactor;
                    if (VRSettings.SmoothWeaponAim())
                        smoothing = VRSettings.GetSmoothingSensitivity();
                    if (VRSettings.GetWeaponWeightOn())
                        smoothing = smoothingFactor / VRGlobals.firearmController.ErgonomicWeight;
                    //rawRightHand.transform.position = Vector3.Slerp(preVelocityRightHandPos, RightHand.transform.position, smoothing * Time.deltaTime);
                    rawRightHand.transform.position = Vector3.Slerp(rawRightHand.transform.position, RightHand.transform.position, smoothing * Time.deltaTime);
                }
                else
                    rawRightHand.transform.position = RightHand.transform.position;

                if ((VRGlobals.firearmController || WeaponPatches.grenadeEquipped || WeaponPatches.rangeFinder) && framesAfterSwitching < 2)
                {
                    VRGlobals.weaponHolder.transform.parent.position = rawRightHand.transform.position;
                    VRGlobals.weaponHolder.transform.parent.rotation = rawRightHand.transform.rotation;
                    framesAfterSwitching++;
                }
                //Plugin.MyLog.LogInfo(RightHand.transform.rotation.eulerAngles + "     |   " + rawRightHand.transform.rotation.eulerAngles + "  |  " + VRGlobals.weaponHolder.transform.rotation.eulerAngles);
            }

        }

        private void UpdateLeftHand(SteamVR_Action_Pose fromAction, SteamVR_Input_Sources fromSource)
        {
            leftHandYRotation = fromAction.localRotation.eulerAngles.y;
            if (!LeftHand || (VRGlobals.handsInteractionController && VRGlobals.handsInteractionController.scopeTransform && SteamVR_Actions._default.LeftGrip.state)) 
                return;


            if (VRGlobals.player && VRGlobals.player.BodyAnimatorCommon.GetFloat(LEFT_HAND_ANIMATOR_HASH) == 1.0)
            {
                if (!leftHandInAnimation)
                {
                    VRGlobals.player._markers[0] = WeaponPatches.previousLeftHandMarker;
                    leftHandInAnimation = true;
                }
                return;
            }
            else if (leftHandInAnimation) {
                if (isSupporting)
                    VRGlobals.player._markers[0] = WeaponPatches.previousLeftHandMarker;
                else
                    VRGlobals.player._markers[0] = LeftHand.transform;

                leftHandInAnimation = false;
            }
            if (leftHandGunIK)
            {
                bool withinDistance = Vector3.Distance(LeftHand.transform.position, leftHandGunIK.position) < 0.1f;


                // If the player is already in support position with snap enabled and they aren't holding grip, check for the position similar to above 
                // but give the distance between hand and IK position some tolerance so the hand doesn't rapidly swap between support pos and non-support pos
                // when in close vicinity.
                bool withinDistanceAfterSnap = withinDistance && isSupporting && Vector3.Distance(LeftHand.transform.position, leftHandGunIK.position) < 0.175f;
                if (withinDistance || handLock || withinDistanceAfterSnap)
                {
                    if (!isSupporting && (!VRSettings.GetSnapToGun() || handLock))
                    {
                        if (isWeapPistol)
                            VRGlobals.player._markers[0] = WeaponPatches.previousLeftHandMarker;
                        initialRightHandRotation = rawRightHand.transform.rotation;
                        // Set left hand target to the original left hand target
                        //VRGlobals.player._markers[0] = WeaponPatches.previousLeftHandMarker;
                        isSupporting = true;
                        if (UIPatches.stancePanel)
                            UIPatches.stancePanel.AnimatedHide();
                        if (UIPatches.healthPanel)    
                            UIPatches.healthPanel.AnimatedHide();
                        // Stance panel is stubborn and still doesn't go away after AnimatedHide sometimes so set it to inactive
                        if (UIPatches.stancePanel)
                            UIPatches.stancePanel.gameObject.SetActive(false);
                        if (UIPatches.extractionTimerUi)
                            UIPatches.extractionTimerUi.Hide();
                    }
                    if (VRSettings.GetSupportGunHoldToggle())
                    {
                        if (SteamVR_Actions._default.LeftGrip.state)
                            handLock = true;
                        else
                            handLock = false;
                    }
                    else
                    {
                        if (!isSupporting && SteamVR_Actions._default.LeftGrip.stateDown)
                            handLock = true;
                        else if (isSupporting && SteamVR_Actions._default.LeftGrip.stateDown) {
                            isSupporting = false;
                            //VRGlobals.player._markers[0] = LeftHand.transform;
                            ////VRGlobals.ikManager.leftArmIk.solver.target = LeftHand.transform;
                            //VRGlobals.weaponHolder.transform.localPosition = WeaponPatches.weaponOffset;
                            //VRGlobals.weaponHolder.transform.localRotation = Quaternion.Euler(15, 275, 90);
                            //VRGlobals.firearmController.WeaponRoot.localPosition = new Vector3(0.1327f, -0.0578f, -0.0105f);
                            handLock = false;
                        }
                    }
                    // This condition should only even happen if snapping to the gun is disabled
                    if (!isSupporting)
                    {
                        SteamVR_Actions._default.Haptic.Execute(0, 0.1f, 1, 0.1f, SteamVR_Input_Sources.LeftHand);
                        Vector3 virtualBasePosition = fromAction.localPosition - fromAction.localRotation * Vector3.forward * controllerLength;
                        LeftHand.transform.localPosition = virtualBasePosition;
                    }
                    else {
                        //Vector3 virtualBasePosition = fromAction.localPosition - fromAction.localRotation * Vector3.forward * controllerLength;
                        //LeftHand.transform.localPosition = virtualBasePosition;
                        LeftHand.transform.localPosition = fromAction.localPosition;
                    }


                }
                else
                {
                    if (isSupporting)
                    {
                        isSupporting = false;
                        if (isWeapPistol)
                            VRGlobals.player._markers[0] = LeftHand.transform;
                        //VRGlobals.player._markers[0] = LeftHand.transform;
                        ////VRGlobals.ikManager.leftArmIk.solver.target = LeftHand.transform;
                        //VRGlobals.weaponHolder.transform.localPosition = WeaponPatches.weaponOffset;
                        //VRGlobals.weaponHolder.transform.localRotation = Quaternion.Euler(15, 275, 90);
                        //VRGlobals.firearmController.WeaponRoot.localPosition = new Vector3(0.1327f, -0.0578f, -0.0105f);

                    }
                    Vector3 virtualBasePosition = fromAction.localPosition - fromAction.localRotation * Vector3.forward * controllerLength;
                    LeftHand.transform.localPosition = virtualBasePosition;
                    //if (leftHandGunIK)
                    //Plugin.MyLog.LogWarning(Vector3.Distance(LeftHand.transform.position, leftHandGunIK.position) + "   |    " + isSupporting);
                    //LeftHand.transform.localPosition = fromAction.localPosition + leftHandOffset;
                    LeftHand.transform.localRotation = fromAction.localRotation;
                    LeftHand.transform.Rotate(-60, 0, 70);
                }
            }
            else {
                Vector3 virtualBasePosition = fromAction.localPosition - fromAction.localRotation * Vector3.forward * controllerLength;
                LeftHand.transform.localPosition = virtualBasePosition;
                LeftHand.transform.localRotation = fromAction.localRotation;
                LeftHand.transform.Rotate(-60, 0, 70 + VRSettings.GetLeftHandHorizontalOffset());
            }
            //else
            //{
            //    LeftHand.transform.localPosition = fromAction.localPosition;
            //}
            if (UIPatches.stancePanel && UIPatches.healthPanel)
            {
                if (!isSupporting)
                {
                    if (!UIPatches.stancePanel.gameObject.active)
                        UIPatches.stancePanel.gameObject.SetActive(true);

                    RaycastHit hit;
                    LayerMask mask = 1 << 7;
                    if (Physics.Raycast(LeftHand.transform.position, LeftHand.transform.up * -1, out hit, 2, mask) && hit.collider.name == "camHolder")
                    {
                        if (!showingHealthUi)
                        {
                            UIPatches.stancePanel.AnimatedShow(false);
                            UIPatches.healthPanel.AnimatedShow(false);
                            //if (UIPatches.quickSlotUi)
                            //    UIPatches.quickSlotUi.active = true;
                            showingHealthUi = true;
                        }

                    }
                    else if (showingHealthUi)
                    {
                        UIPatches.stancePanel.AnimatedHide();
                        UIPatches.healthPanel.AnimatedHide();
                        //if (UIPatches.quickSlotUi)
                        //    UIPatches.quickSlotUi.active = false;
                        showingHealthUi = false;
                    }
                }
                else if (showingHealthUi) {
                    UIPatches.stancePanel.AnimatedHide();
                    UIPatches.healthPanel.AnimatedHide();
                    showingHealthUi = false;
                }
            }
            if (UIPatches.extractionTimerUi)
            {
                if (!isSupporting) { 
                    RaycastHit hit;
                    LayerMask mask = 1 << 7;
                    if (Physics.Raycast(LeftHand.transform.position, LeftHand.transform.up * 1, out hit, 2, mask) && hit.collider.name == "camHolder")
                    {
                        if (!showingExtractionUi)
                        {
                            UIPatches.extractionTimerUi.Reveal();
                            UIPatches.extractionTimerUi.ShowTimer(true, true);
                            showingExtractionUi = true;
                        }
                    }
                    else if (showingExtractionUi)
                    {
                        UIPatches.extractionTimerUi.Hide();
                        showingExtractionUi = false;
                    }
                }
                else if (showingExtractionUi) { 
                    UIPatches.extractionTimerUi.Hide();
                    showingExtractionUi = false;
                }
            }
        }

        protected void SpawnHands()
        {
            if (!RightHand && VRGlobals.menuVRManager.RightHand)
                RightHand = VRGlobals.menuVRManager.RightHand;
            if (!LeftHand && VRGlobals.menuVRManager.LeftHand)
                LeftHand = VRGlobals.menuVRManager.LeftHand;

            if (!rawRightHand) { 

                rawRightHand = new GameObject("rawRightHand").transform;
                rawRightHand.transform.parent = VRGlobals.vrOffsetter.transform;
            }
        }


        // REMOVE BEFOREGBuFFER maybe

    }


}


