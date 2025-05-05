using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using ExitGames.Client.Photon;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace LateJoin
{
    [BepInPlugin("spindles.privatelatejoin", MOD_NAME, "0.1.4")]
    internal sealed class Entry : BaseUnityPlugin
    {
        private const string MOD_NAME = "Private Late Join";

        internal static readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(MOD_NAME);
        
        private static readonly FieldInfo removeFilterFieldInfo = AccessTools.Field(typeof(PhotonNetwork), "removeFilter");
        private static readonly FieldInfo keyByteSevenFieldInfo = AccessTools.Field(typeof(PhotonNetwork), "keyByteSeven");
        private static readonly FieldInfo serverCleanOptionsFieldInfo = AccessTools.Field(typeof(PhotonNetwork), "ServerCleanOptions");
        private static readonly MethodInfo raiseEventInternalMethodInfo = AccessTools.Method(typeof(PhotonNetwork), "RaiseEventInternal");
        
        private static void RunManager_ChangeLevelHook(Action<RunManager, bool, bool, RunManager.ChangeLevelType> orig, RunManager self, bool _completedLevel, bool _levelFailed, RunManager.ChangeLevelType _changeLevelType)
        {
            if (_levelFailed || !PhotonNetwork.IsMasterClient)
            {
                orig.Invoke(self, _completedLevel, _levelFailed, _changeLevelType);
                return;
            }
            
            var runManagerPUN = AccessTools.Field(typeof(RunManager), "runManagerPUN").GetValue(self);
            var runManagerPhotonView = AccessTools.Field(typeof(RunManagerPUN), "photonView").GetValue(runManagerPUN) as PhotonView;
            
            PhotonNetwork.RemoveBufferedRPCs(runManagerPhotonView!.ViewID);

            foreach (var photonView in FindObjectsOfType<PhotonView>())
            {
                if (photonView.gameObject.scene.buildIndex == -1)
                    continue;
                
                ClearPhotonCache(photonView);
            }
            
            orig.Invoke(self, _completedLevel, false, _changeLevelType);
            
            var canJoin = SemiFunc.RunIsLobbyMenu() || SemiFunc.RunIsLobby(); // SemiFunc.RunIsShop()

			if (canJoin) 
			{
				if (typeof(SteamManager).GetRuntimeMethod("UnlockLobby", [typeof(bool)]) != null)
					typeof(SteamManager).GetRuntimeMethod("UnlockLobby", [typeof(bool)]).Invoke(SteamManager.instance, [true]);
				else
					typeof(SteamManager).GetRuntimeMethod("UnlockLobby", [])?.Invoke(SteamManager.instance, []);
			}
            else
                SteamManager.instance.LockLobby();
            
            PhotonNetwork.CurrentRoom.IsOpen = canJoin;
        }

        private static void PlayerAvatar_SpawnHook(Action<PlayerAvatar, Vector3, Quaternion> orig, PlayerAvatar self, Vector3 position, Quaternion rotation)
        {
            if ((bool) AccessTools.Field(typeof(PlayerAvatar), "spawned").GetValue(self))
                return;
            
            orig.Invoke(self, position, rotation);
        }
        
        private static void LevelGenerator_StartHook(Action<LevelGenerator> orig, LevelGenerator self)
        {
            if (PhotonNetwork.IsMasterClient && SemiFunc.RunIsShop() || SemiFunc.RunIsLobby())
                PhotonNetwork.RemoveBufferedRPCs(self.PhotonView.ViewID);
            
            orig.Invoke(self);
        }
        
        private static void PlayerAvatar_StartHook(Action<PlayerAvatar> orig, PlayerAvatar self)
        {
            orig.Invoke(self);

            if (!PhotonNetwork.IsMasterClient && !SemiFunc.RunIsLobby() || !SemiFunc.RunIsShop())
                return;
            
            self.photonView.RPC("LoadingLevelAnimationCompletedRPC", RpcTarget.AllBuffered);
        }

        private static void ClearPhotonCache(PhotonView photonView)
        {
            var removeFilter = removeFilterFieldInfo.GetValue(null) as Hashtable;
            var keyByteSeven = keyByteSevenFieldInfo.GetValue(null);
            var serverCleanOptions = serverCleanOptionsFieldInfo.GetValue(null) as RaiseEventOptions;
            
            removeFilter![keyByteSeven] = photonView.InstantiationId;
            serverCleanOptions!.CachingOption = EventCaching.RemoveFromRoomCache;
            raiseEventInternalMethodInfo.Invoke(null, [(byte) 202, removeFilter, serverCleanOptions, SendOptions.SendReliable]);
        }
        
        private void Awake()
        {
			logger.LogDebug("Hooking `RunManager.ChangeLevel`");
            new Hook(AccessTools.Method(typeof(RunManager), "ChangeLevel"), RunManager_ChangeLevelHook);
            
            logger.LogDebug("Hooking `PlayerAvatar.Spawn`");
            new Hook(AccessTools.Method(typeof(PlayerAvatar), "Spawn"), PlayerAvatar_SpawnHook);

            logger.LogDebug("Hooking `LevelGenerator.Start`");
            new Hook(AccessTools.Method(typeof(LevelGenerator), "Start"), LevelGenerator_StartHook);
            
            logger.LogDebug("Hooking `PlayerAvatar.Start`");
            new Hook(AccessTools.Method(typeof(PlayerAvatar), "Start"), PlayerAvatar_StartHook);
            
            // Since we're not currently loading anything but the shop,
            // hooks below are unnecessary at the moment.
            
            /* logger.LogDebug("Hooking `PlayerAvatar.OnDestroy`");
            new Hook(AccessTools.Method(typeof(PlayerAvatar), "OnDestroy"), PlayerAvatar_OnDestroyHook); */
        }
    }
}





/* private static void PlayerAvatar_OnDestroyHook(Action<PlayerAvatar> orig, PlayerAvatar self)
        {
            orig.Invoke(self);

            if (!PhotonNetwork.IsMasterClient || !SemiFunc.RunIsLobby()) // && !SemiFunc.RunIsShop()
                return;

            foreach (var photonView in FindObjectsOfType<PhotonView>())
            {
                if (photonView?.transform.parent && photonView.transform.parent.name is not "Enemies")
                    continue;

                ClearPhotonCache(photonView);
            }
        }*/
