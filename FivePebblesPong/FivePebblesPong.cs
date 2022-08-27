using BepInEx;
using System;
using RWCustom;
using UnityEngine;

namespace FivePebblesPong
{
    public static class EnumExt_FPP //dependency: EnumExtender.dll
    {
        public static AbstractPhysicalObject.AbstractObjectType FPGameController;
    }


    [BepInPlugin("author.my_mod_id", "FivePebblesPong", "0.1.0")]	// (GUID, mod name, mod version)
    public class FivePebblesPong : BaseUnityPlugin
    {
        //https://rainworldmodding.miraheze.org/wiki/Code_Environments
        private static WeakReference __me; //WeakReference still allows garbage collection
        public FivePebblesPong() { __me = new WeakReference(this); }
        public static FivePebblesPong ME => __me?.Target as FivePebblesPong;

        public BepInEx.Logging.ManualLogSource Logger_p => Logger;
        public static bool HasEnumExt => (int)EnumExt_FPP.FPGameController > 0; //returns true after EnumExtender initializes


        public void OnEnable() //called when mod is loaded, subscribe functions to methods of the game
        {
            Hooks.Apply();
            Logger.LogInfo("OnEnable()");
        }
    }
}
