﻿using UnityEngine;
using Valve.VR;
using TarkovVR.Source.Player.VRManager;
using TarkovVR.Patches.UI;

namespace TarkovVR.Source.Player.Interactions
{
    internal class HandsInteractionController : MonoBehaviour
    {
        public Quaternion initialHandRot;

        public bool swapWeapon = false;
        private bool changingScopeZoom = false;
        public void Update()
        {

            Collider[] nearbyColliders = Physics.OverlapSphere(VRGlobals.vrPlayer.RightHand.transform.position, 0.125f);
            if (!VRGlobals.vrPlayer.isSupporting) {
                swapWeapon = false;
                foreach (Collider collider in nearbyColliders)
                {
                    if (collider.gameObject.layer == 3 && collider.gameObject.name == "backHolsterCollider")
                    {
                        SteamVR_Actions._default.Haptic.Execute(0, 0.1f, 1, 0.4f, SteamVR_Input_Sources.RightHand);
                        if (SteamVR_Actions._default.RightGrip.stateDown)
                        {
                            swapWeapon = true;
                        }
                    }
                }
            }

            nearbyColliders = Physics.OverlapSphere(VRGlobals.vrPlayer.LeftHand.transform.position, 0.125f);

            foreach (Collider collider in nearbyColliders)
            {
                if (collider.gameObject.layer == 6)
                {
                    handleScopeInteraction();
                }
                else if (collider.gameObject.layer == 3 && collider.gameObject.name == "rigCollider")
                {
                    SteamVR_Actions._default.Haptic.Execute(0, 0.1f, 1, 0.4f, SteamVR_Input_Sources.LeftHand);
                    if (UIPatches.quickSlotUi && SteamVR_Actions._default.LeftGrip.stateDown) { 
                        UIPatches.quickSlotUi.active = true;
                    }
                }
            }
            if (changingScopeZoom)
                handleScopeInteraction();
        }

        private void handleScopeInteraction()
        {
            if (SteamVR_Actions._default.LeftGrip.stateDown)
            {
                VRGlobals.vrOpticController.initZoomDial();
                changingScopeZoom = true;
            }
            if (SteamVR_Actions._default.LeftGrip.state)
            {
                VRGlobals.vrOpticController.handleZoomDial();
            }
            else
            {
                if (changingScopeZoom)
                    changingScopeZoom = false;
                SteamVR_Actions._default.Haptic.Execute(0, 0.1f, 1, 0.2f, SteamVR_Input_Sources.LeftHand);
            }
        }

    }
}
