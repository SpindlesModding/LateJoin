using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Photon.Pun;

namespace LateJoin
{
    [BepInPlugin("nickklmao.latejoin", MOD_NAME, "1.0.0")]
    internal sealed class Entry : BaseUnityPlugin
    {
        private const string MOD_NAME = "Late Join";

        internal static readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(MOD_NAME);

        private static void RunManager_ChangeLevelHook(Action<RunManager, bool, bool, RunManager.ChangeLevelType> orig, RunManager self, bool _completedLevel, bool _levelFailed, RunManager.ChangeLevelType _changeLevelType)
        {
            orig.Invoke(self, _completedLevel, _levelFailed, _changeLevelType);
            
            if (_levelFailed || !PhotonNetwork.IsMasterClient)
                return;

            var canJoin = SemiFunc.RunIsLobby() || SemiFunc.RunIsLobbyMenu(); 
            
            if (canJoin)
                SteamManager.instance.UnlockLobby();
            else
                SteamManager.instance.LockLobby();
            
            PhotonNetwork.CurrentRoom.IsOpen = canJoin;
        }

        private static void LevelGenerator_PlayerSpawnILHook(ILContext il)
        {
            var cursor = new ILCursor(il);

            cursor.GotoNext(instruction => instruction.MatchStloc(7), instruction => instruction.MatchLdsfld<GameDirector>("instance"));

            cursor.Index++;

            cursor.RemoveRange(36);
        }
        
        private void Awake()
        {
            new Hook(AccessTools.Method(typeof(RunManager), "ChangeLevel"), RunManager_ChangeLevelHook);
            new ILHook(AccessTools.Method(typeof(LevelGenerator), "PlayerSpawn"), LevelGenerator_PlayerSpawnILHook);
        }
    }
}