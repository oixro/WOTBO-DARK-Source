using System.Collections.Generic;
using System;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Power latency: значения Control\Power и GraphicsDrivers\Power.</summary>
    public sealed class PowerLatencyTweak : TweakBase
    {
        private static readonly KeyValuePair<string, int>[] PowerValues =
        {
            new KeyValuePair<string, int>("ExitLatency", 0x1),
            new KeyValuePair<string, int>("DisableVsyncLatencyUpdate", 0x1),
            new KeyValuePair<string, int>("DisableSensorWatchdog", 0x1),
            new KeyValuePair<string, int>("ExitLatencyCheckEnabled", 0x1),
            new KeyValuePair<string, int>("Latency", 0x1),
            new KeyValuePair<string, int>("LatencyToleranceDefault", 0x1),
            new KeyValuePair<string, int>("LatencyToleranceFSVP", 0x1),
            new KeyValuePair<string, int>("LatencyToleranceIdleResiliency", 0x1),
            new KeyValuePair<string, int>("LatencyTolerancePerfOverride", 0x1),
            new KeyValuePair<string, int>("LatencyToleranceScreenOffIR", 0x1),
            new KeyValuePair<string, int>("LatencyToleranceVSyncEnabled", 0x1),
            new KeyValuePair<string, int>("RtlCapabilityCheckLatency", 0x1),
            new KeyValuePair<string, int>("MfBufferingThreshold", 0x0),
            new KeyValuePair<string, int>("CsEnabled", 0x0),
            new KeyValuePair<string, int>("QosManagesIdleProcessors", 0x0),
            new KeyValuePair<string, int>("SleepReliabilityDetailedDiagnostics", 0x0),
            new KeyValuePair<string, int>("EventProcessorEnabled", 0x0),
        };

        private static readonly KeyValuePair<string, int>[] GraphicsValues =
        {
            new KeyValuePair<string, int>("RmGpsPsEnablePerCpuCoreDpc", 0x1),
            new KeyValuePair<string, int>("PowerSavingTweaks", 0x0),
            new KeyValuePair<string, int>("DisableWriteCombining", 0x1),
            new KeyValuePair<string, int>("EnableRuntimePowerManagement", 0x0),
            new KeyValuePair<string, int>("PrimaryPushBufferSize", 0x1),
            new KeyValuePair<string, int>("FlTransitionLatency", 0x0),
            new KeyValuePair<string, int>("D3PCLatency", 0x0),
            new KeyValuePair<string, int>("RMDeepLlEntryLatencyUsec", 0x0),
            new KeyValuePair<string, int>("PciLatencyTimerControl", 0x1),
            new KeyValuePair<string, int>("Node3DLowLatency", 0x1),
            new KeyValuePair<string, int>("LOWLATENCY", 0x1),
            new KeyValuePair<string, int>("RmDisableRegistryCaching", 0x1),
            new KeyValuePair<string, int>("RMDisablePostL2Compression", 0x1),
            new KeyValuePair<string, int>("DefaultMemoryRefreshLatencyToleranceNoContext", 0x1),
            new KeyValuePair<string, int>("DefaultMemoryRefreshLatencyToleranceMonitorOff", 0x1),
            new KeyValuePair<string, int>("DefaultMemoryRefreshLatencyToleranceActivelyUsed", 0x1),
            new KeyValuePair<string, int>("DefaultD3TransitionLatencyIdleShortTime", 0x1),
            new KeyValuePair<string, int>("DefaultD3TransitionLatencyIdleNoContext", 0x1),
            new KeyValuePair<string, int>("DefaultD3TransitionLatencyIdleMonitorOff", 0x1),
            new KeyValuePair<string, int>("DefaultLatencyToleranceTimerPeriod", 0x1),
            new KeyValuePair<string, int>("DefaultLatencyToleranceOther", 0x1),
            new KeyValuePair<string, int>("DefaultLatencyToleranceNoContextMonitorOff", 0x1),
            new KeyValuePair<string, int>("DefaultLatencyToleranceNoContext", 0x1),
            new KeyValuePair<string, int>("DefaultLatencyToleranceMemory", 0x1),
            new KeyValuePair<string, int>("DefaultLatencyToleranceIdle1MonitorOff", 0x1),
            new KeyValuePair<string, int>("DefaultLatencyToleranceIdle1", 0x1),
            new KeyValuePair<string, int>("DefaultLatencyToleranceIdle0MonitorOff", 0x1),
            new KeyValuePair<string, int>("DefaultLatencyToleranceIdle0", 0x1),
            new KeyValuePair<string, int>("DefaultD3TransitionLatencyIdleVeryLongTime", 0x1),
            new KeyValuePair<string, int>("DefaultD3TransitionLatencyIdleLongTime", 0x1),
            new KeyValuePair<string, int>("DefaultD3TransitionLatencyActivelyUsed", 0x1),
            new KeyValuePair<string, int>("Latency", 0x1),
            new KeyValuePair<string, int>("TransitionLatency", 0x1),
            new KeyValuePair<string, int>("MonitorRefreshLatencyTolerance", 0x1),
            new KeyValuePair<string, int>("MonitorLatencyTolerance", 0x1),
            new KeyValuePair<string, int>("MiracastPerfTrackGraphicsLatency", 0x1),
            new KeyValuePair<string, int>("MaxIAverageGraphicsLatencyInOneBucket", 0x1),
        };

        public override string Id
        {
            get { return "main.powerlatency"; }
        }

        public override string Category
        {
            get { return "Main"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            object current = Read(
                RegistryHive.LocalMachine,
                RegistryPaths.Power,
                "ExitLatency",
                context);

            return string.Equals(Convert.ToString(current), "1", StringComparison.Ordinal)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            foreach (KeyValuePair<string, int> value in PowerValues)
            {
                context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Power, value.Key, value.Value, RegistryValueKind.DWord);
            }

            foreach (KeyValuePair<string, int> value in GraphicsValues)
            {
                context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.GraphicsPower, value.Key, value.Value, RegistryValueKind.DWord);
            }
        }

        public override void Rollback(TweakContext context)
        {
            foreach (KeyValuePair<string, int> value in PowerValues)
            {
                if (value.Key == "EventProcessorEnabled")
                {
                    context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Power, value.Key, 1, RegistryValueKind.DWord);
                }
                else
                {
                    context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.Power, value.Key);
                }
            }

            foreach (KeyValuePair<string, int> value in GraphicsValues)
            {
                context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.GraphicsPower, value.Key);
            }
        }
    }
}
