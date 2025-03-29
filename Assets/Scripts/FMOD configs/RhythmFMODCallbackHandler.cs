using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Rhythm FMOD Callback Handler")]
public class RhythmFMODCallbackHandler : FMODUnity.PlatformCallbackHandler
{
    public override void PreInitialize(FMOD.Studio.System studioSystem, Action<FMOD.RESULT, string> reportResult)
    {
        FMOD.RESULT result;

        FMOD.System coreSystem;
        result = studioSystem.getCoreSystem(out coreSystem);
        reportResult(result, "studioSystem.getCoreSystem");

        // Set up studioSystem and coreSystem as desired
        FMOD.Studio.ADVANCEDSETTINGS studioAdvancedSettings = new FMOD.Studio.ADVANCEDSETTINGS();
        studioAdvancedSettings.studioupdateperiod = 10;
        result = studioSystem.setAdvancedSettings(studioAdvancedSettings);
    }
}
