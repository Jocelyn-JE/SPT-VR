﻿using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace TarkovVR.Patches.Visuals
{
    [HarmonyPatch]
    internal class VisualPatches
    {
        private static Camera postProcessingStoogeCamera;

        // NOTEEEEEE: You can completely delete SSAA and SSAAPropagatorOpaque and the blurriness still occcurs so it must be from SSAAPropagator or SSAAImpl

        //------------------------------------------------------------------------------------------------------------------------------------------------------------
        // These two functions would return the screen resolution setting and would result in the game
        // being very blurry
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SSAAImpl), "GetOutputWidth")]
        private static bool ReturnVROutputWidth(SSAAImpl __instance, ref int __result)
        {
            __result = __instance.GetInputWidth();
            return false;
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Holy fuck this actually fixes so many visual problems :)
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SSAAPropagator), "OnRenderImage")]
        private static bool ReturnVROutputWidth(SSAAPropagator __instance, RenderTexture source, RenderTexture destination)
        {
            if (__instance._postProcessLayer != null)
            {
                Graphics.Blit(source, destination);
                return false;
            }
            __instance._currentDestinationHDR = 0;
            __instance._currentDestinationLDR = 0;
            __instance._HDRSourceDestination = true;
            int width = Camera.main.pixelWidth;
            int height = Camera.main.pixelHeight;
            if (__instance._resampledColorTargetHDR[0] == null || __instance._resampledColorTargetHDR[0].width != width || __instance._resampledColorTargetHDR[0].height != height || __instance._resampledColorTargetHDR[0].format != RuntimeUtilities.defaultHDRRenderTextureFormat)
            {
                if (__instance._resampledColorTargetHDR[0] != null)
                {
                    __instance._resampledColorTargetHDR[0].Release();
                    RuntimeUtilities.SafeDestroy(__instance._resampledColorTargetHDR[0]);
                    __instance._resampledColorTargetHDR[0] = null;
                }
                if (__instance._resampledColorTargetHDR[1] != null)
                {
                    __instance._resampledColorTargetHDR[1].Release();
                    RuntimeUtilities.SafeDestroy(__instance._resampledColorTargetHDR[1]);
                    __instance._resampledColorTargetHDR[1] = null;
                }
                RenderTextureFormat defaultHDRRenderTextureFormat = RuntimeUtilities.defaultHDRRenderTextureFormat;
                __instance._resampledColorTargetHDR[0] = new RenderTexture(width, height, 0, defaultHDRRenderTextureFormat);
                __instance._resampledColorTargetHDR[0].name = "SSAAPropagator0HDR";
                __instance._resampledColorTargetHDR[0].enableRandomWrite = true;
                __instance._resampledColorTargetHDR[0].Create();
                __instance._resampledColorTargetHDR[1] = new RenderTexture(width, height, 0, defaultHDRRenderTextureFormat);
                __instance._resampledColorTargetHDR[1].name = "SSAAPropagator1HDR";
                __instance._resampledColorTargetHDR[1].enableRandomWrite = true;
                __instance._resampledColorTargetHDR[1].Create();
            }
            if (__instance._resampledColorTargetLDR[0] == null || __instance._resampledColorTargetLDR[0].width != width || __instance._resampledColorTargetLDR[0].height != height)
            {
                if (__instance._resampledColorTargetLDR[0] != null)
                {
                    __instance._resampledColorTargetLDR[0].Release();
                    RuntimeUtilities.SafeDestroy(__instance._resampledColorTargetLDR[0]);
                    __instance._resampledColorTargetLDR[0] = null;
                }
                if (__instance._resampledColorTargetLDR[1] != null)
                {
                    __instance._resampledColorTargetLDR[1].Release();
                    RuntimeUtilities.SafeDestroy(__instance._resampledColorTargetLDR[1]);
                    __instance._resampledColorTargetLDR[1] = null;
                }
                if (__instance._resampledColorTargetLDR[2] != null)
                {
                    __instance._resampledColorTargetLDR[2].Release();
                    RuntimeUtilities.SafeDestroy(__instance._resampledColorTargetLDR[2]);
                    __instance._resampledColorTargetLDR[2] = null;
                }
                RenderTextureFormat format = RenderTextureFormat.ARGB32;
                __instance._resampledColorTargetLDR[0] = new RenderTexture(width, height, 0, format);
                __instance._resampledColorTargetLDR[1] = new RenderTexture(width, height, 0, format);
                __instance._resampledColorTargetLDR[1].name = "SSAAPropagator1LDR";
                __instance._resampledColorTargetLDR[2] = new RenderTexture(width, height, 0, format);
                __instance._resampledColorTargetLDR[2].name = "Stub";
            }
            if ((double)Mathf.Abs(__instance.m_ssaa.GetCurrentSSRatio() - 1f) < 0.001)
            {
                Graphics.Blit(source, __instance._resampledColorTargetHDR[0]);
            }
            else if (__instance.m_ssaa.GetCurrentSSRatio() > 1f)
            {
                __instance.m_ssaa.RenderImage(__instance.m_ssaa.GetRT(), __instance._resampledColorTargetHDR[0], flipV: true, null);
            }
            else
            {
                __instance.m_ssaa.RenderImage(source, __instance._resampledColorTargetHDR[0], flipV: true, null);
            }
            if (__instance._cmdBuf == null)
            {
                __instance._cmdBuf = new CommandBuffer();
                __instance._cmdBuf.name = "SSAAPropagator";
            }
            __instance._cmdBuf.Clear();
            if (!__instance._thermalVisionIsOn && (__instance._opticLensRenderer != null || __instance._collimatorRenderer != null))
            {
                if (__instance._resampledDepthTarget == null || __instance._resampledDepthTarget.width != width || __instance._resampledDepthTarget.height != height)
                {
                    if (__instance._resampledDepthTarget != null)
                    {
                        __instance._resampledDepthTarget.Release();
                        RuntimeUtilities.SafeDestroy(__instance._resampledDepthTarget);
                        __instance._resampledDepthTarget = null;
                    }
                    __instance._resampledDepthTarget = new RenderTexture(width, height, 24, RenderTextureFormat.Depth);
                    __instance._resampledDepthTarget.name = "SSAAPropagatorDepth";
                }
                __instance._cmdBuf.BeginSample("OutputOptic");
                __instance._cmdBuf.EnableShaderKeyword(SSAAPropagator.KWRD_TAA);
                __instance._cmdBuf.EnableShaderKeyword(SSAAPropagator.KWRD_NON_JITTERED);
                __instance._cmdBuf.SetGlobalMatrix(SSAAPropagator.ID_NONJITTEREDPROJ, GL.GetGPUProjectionMatrix(__instance._camera.nonJitteredProjectionMatrix, renderIntoTexture: true));
                __instance._cmdBuf.SetRenderTarget(__instance._resampledColorTargetHDR[0], __instance._resampledDepthTarget);
                __instance._cmdBuf.ClearRenderTarget(clearDepth: true, clearColor: false, Color.black);
                if (__instance._opticLensRenderer == null && __instance._collimatorRenderer != null)
                {
                    __instance._cmdBuf.DrawRenderer(__instance._collimatorRenderer, __instance._collimatorMaterial);
                }
                if (__instance._opticLensRenderer != null)
                {
                    if (__instance._sightNonLensRenderers != null && __instance._sightNonLensRenderers.Length != 0)
                    {
                        __instance._cmdBuf.BeginSample("DEPTH_PREPASS");
                        __instance._cmdBuf.SetRenderTarget(__instance._resampledColorTargetLDR[2], __instance._resampledDepthTarget);
                        __instance._cmdBuf.BeginSample("SIGHT_DEPTH");
                        for (int i = 0; i < __instance._sightNonLensRenderers.Length; i++)
                        {
                            if (__instance._sightNonLensRenderers[i] != null && __instance._sightNonLensRenderersMaterials[i] != null && __instance._sightNonLensRenderers[i].gameObject.activeSelf)
                            {
                                __instance._cmdBuf.DrawRenderer(__instance._sightNonLensRenderers[i], __instance._sightNonLensRenderersMaterials[i]);
                            }
                        }
                        __instance._cmdBuf.EndSample("SIGHT_DEPTH");
                        __instance._cmdBuf.BeginSample("WEAPON_DEPTH");
                        for (int j = 0; j < __instance._otherWeaponRenderers.Length; j++)
                        {
                            if (__instance._otherWeaponRenderers[j] != null && __instance._otherWeaponRenderersMaterials[j] != null && __instance._otherWeaponRenderers[j].gameObject.activeSelf)
                            {
                                __instance._cmdBuf.DrawRenderer(__instance._otherWeaponRenderers[j], __instance._otherWeaponRenderersMaterials[j]);
                            }
                        }
                        __instance._cmdBuf.EndSample("WEAPON_DEPTH");
                        __instance._cmdBuf.EndSample("DEPTH_PREPASS");
                    }
                    __instance._cmdBuf.SetRenderTarget(__instance._resampledColorTargetHDR[0], __instance._resampledDepthTarget);
                    __instance._cmdBuf.DrawRenderer(__instance._opticLensRenderer, __instance._opticLensMaterial);
                }
                __instance._cmdBuf.SetRenderTarget(destination);
                __instance._cmdBuf.DisableShaderKeyword(SSAAPropagator.KWRD_NON_JITTERED);
                __instance._cmdBuf.DisableShaderKeyword(SSAAPropagator.KWRD_TAA);
                __instance._cmdBuf.EndSample("OutputOptic");
            }
            if ((bool)__instance._nightVisionMaterial)
            {
                __instance._cmdBuf.EnableShaderKeyword(SSAAPropagator.KWRD_NIGHTVISION_NOISE);
                __instance._cmdBuf.Blit(__instance._resampledColorTargetHDR[0], __instance._resampledColorTargetHDR[1], __instance._nightVisionMaterial);
                __instance._cmdBuf.DisableShaderKeyword(SSAAPropagator.KWRD_NIGHTVISION_NOISE);
                __instance._currentDestinationHDR = 1;
            }
            else if (__instance._thermalVisionIsOn && __instance._thermalVisionMaterial != null)
            {
                int pass = 1;
                __instance._cmdBuf.Blit(__instance._resampledColorTargetHDR[0], __instance._resampledColorTargetHDR[1], __instance._thermalVisionMaterial, pass);
                __instance._currentDestinationHDR = 1;
            }
            Graphics.ExecuteCommandBuffer(__instance._cmdBuf);
            return false;
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SSAAImpl), "GetOutputHeight")]
        private static bool ReturnVROutputHeight(SSAAImpl __instance, ref int __result)
        {    
            __result = __instance.GetInputHeight();
            return false;
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SSAA), "Awake")]
        private static void DisableSSAA(SSAA __instance)
        {
            __instance.FlippedV = true;

        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PostProcessLayer), "InitLegacy")]
        private static void FixPostProcessing(PostProcessLayer __instance)
        {
            Object.Destroy(__instance);
            //if (VRGlobals.camHolder && VRGlobals.camHolder.GetComponent<Camera>() == null)
            //{
            //    postProcessingStoogeCamera = VRGlobals.camHolder.AddComponent<Camera>();
            //    postProcessingStoogeCamera.enabled = false;
            //}
            //if (postProcessingStoogeCamera)
            //    __instance.m_Camera = postProcessingStoogeCamera;
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------
        // The volumetric light was only using the projection matrix for one eye which made it appear
        // off position in the other eye, this gets the current eyes matrix to fix this issue
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
        //------------------------------------------------------------------------------------------------------------------------------------------------------------
        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryBlur), "Enable")]
        private static void DisableInvBlur(InventoryBlur __instance)
        {
            __instance.enabled = false;
        }




        //------------------------------------------------------------------------------------------------------------------------------------------------------------

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


        //------------------------------------------------------------------------------------------------------------------------------------------------------------
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

        // SSAA causes a bunch of issues like thermal/nightvision rendering all fucky, and the scopes also render in 
        // with 2 other lenses on either side of the main lense, Although SSAA is nice for fixing the jagged edges, it 
        // also adds a strong layer of blur over everything so it's definitely best to keep it disabled. Might look into
        // keeping it around later on if I can figure a way to get it to look nice without messing with everything else

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
        // POST FX - Turning it off gains about 8-10 FPS
    }
}