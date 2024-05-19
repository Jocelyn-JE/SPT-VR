﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TarkovVR.Input;
using TarkovVR;
using UnityEngine;
using Valve.VR;
using HarmonyLib;

namespace TarkovVR
{
    internal class HandsInteractionController : MonoBehaviour
    {
        public Quaternion initialHandRot;

        public bool swapWeapon = false;
        public void Update()
        {

            Collider[] nearbyColliders = Physics.OverlapSphere(CameraManager.RightHand.transform.position, 0.125f);
            swapWeapon = false;
            foreach (Collider collider in nearbyColliders)
            {
                if (collider.gameObject.layer == 3)
                {
                    if (!CamPatches.cameraManager.isSupporting) { 
                        SteamVR_Actions._default.Haptic.Execute(0, 0.1f, 1, 0.4f, SteamVR_Input_Sources.RightHand);
                        if (collider.gameObject.name == "backHolsterCollider" && SteamVR_Actions._default.RightGrip.state) { 
                            swapWeapon = true;
                        }
                    }
                }
            }

            nearbyColliders = (Physics.OverlapSphere(CameraManager.LeftHand.transform.position, 0.125f));

            foreach (Collider collider in nearbyColliders)
            {
                if (collider.gameObject.layer == 6)
                {
                    handleScopeInteraction();
                }
            }
        }

        private void handleScopeInteraction() {
            if (SteamVR_Actions._default.LeftGrip.stateDown)
            {
                CamPatches.vrOpticController.initZoomDial();
            }
            if (SteamVR_Actions._default.LeftGrip.state)
            {
                CamPatches.vrOpticController.handleZoomDial();
            }
            else {
                SteamVR_Actions._default.Haptic.Execute(0, 0.1f, 1, 0.2f, SteamVR_Input_Sources.LeftHand);
            }
        }

    }
}
