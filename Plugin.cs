﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using TarkovVR.ModSupport;
using Unity.XR.OpenVR;
using UnityEngine;
using UnityEngine.XR.Management;
using Valve.VR;

namespace TarkovVR
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource MyLog;
        private bool vrInitializedSuccessfully = false;

        private void Awake()
        {
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loading!");
            MyLog = Logger;

            if (!InitializeVR())
            {
                Logger.LogError("VR initialization failed. Skipping the rest of the plugin setup.");
                return;
            }

            Logger.LogInfo("VR initialized successfully.");
            vrInitializedSuccessfully = true;

            ApplyPatches("TarkovVR.Patches");
            InitializeConditionalPatches();
        }

        private bool InitializeVR()
        {
            try
            {
                SteamVR_Actions.PreInitialize();
                SteamVR_Settings.instance.pauseGameWhenDashboardVisible = true;

                var generalSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                var managerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
                var xrLoader = ScriptableObject.CreateInstance<OpenVRLoader>();

                var settings = OpenVRSettings.GetSettings();
                settings.StereoRenderingMode = OpenVRSettings.StereoRenderingModes.MultiPass;
                generalSettings.Manager = managerSettings;

                managerSettings.loaders.Clear();
                managerSettings.loaders.Add(xrLoader);
                managerSettings.InitializeLoaderSync();

                XRGeneralSettings.AttemptInitializeXRSDKOnLoad();
                XRGeneralSettings.AttemptStartXRSDKOnBeforeSplashScreen();

                // Initialize SteamVR
                SteamVR.Initialize();

                // Verify SteamVR is running
                if (!SteamVR.active)
                {
                    Logger.LogError("[SteamVR] Initialization failed. SteamVR is not active.");
                    return false;
                }

                // Verify OpenVR initialization
                if (SteamVR.instance == null || SteamVR.instance.hmd == null)
                {
                    Logger.LogError("[OpenVR] HMD not found or OpenVR initialization failed.");
                    return false;
                }

                Logger.LogInfo("[VR] Initialization completed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[VR Initialization Error] {ex.Message}");
                return false;
            }
        }


        private void InitializeConditionalPatches()
        {
            if (!vrInitializedSuccessfully)
                return; // Skip patching if VR failed to initialize

            string modDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BepInEx\\plugins\\kmyuhkyuk-EFTApi\\EFTConfiguration.dll");
            if (File.Exists(modDllPath))
            {
                Assembly modAssembly = Assembly.LoadFrom(modDllPath);
                Type configViewType = modAssembly.GetType("EFTConfiguration.Views.EFTConfigurationView");
                if (configViewType != null)
                {
                    InstalledMods.EFTApiInstalled = true;
                    ApplyPatches("TarkovVR.ModSupport.EFTApi");
                    MyLog.LogInfo("Dependent mod found and patches applied.");
                }
                else
                {
                    MyLog.LogWarning("Required types/methods not found in the dependent mod.");
                }
            }
            else
            {
                MyLog.LogWarning("Dependent mod DLL not found. Some functionality will be disabled.");
            }

            // Repeat for other mods (AmandsGraphics, FIKA) as needed
        }

        private void ApplyPatches(string @namespace)
        {
            if (!vrInitializedSuccessfully)
                return; // Skip patching if VR failed to initialize

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            var assembly = Assembly.GetExecutingAssembly();

            foreach (var type in assembly.GetTypes())
            {
                if (type.Namespace != null && type.Namespace.StartsWith(@namespace))
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
            }
        }
    }
}
