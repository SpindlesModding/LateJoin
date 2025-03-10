using System;
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
    [BepInPlugin("nickklmao.latejoin", MOD_NAME, "1.0.0")]
    internal sealed class Entry : BaseUnityPlugin
    {
        private const string MOD_NAME = "Late Join";

        internal static readonly ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(MOD_NAME);

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
                    
                RemoveFromPhotonCache(photonView);
            }
            
            orig.Invoke(self, _completedLevel, true, _changeLevelType);
            
            var canJoin = SemiFunc.RunIsLobbyMenu() || SemiFunc.RunIsShop() || SemiFunc.RunIsLobby(); 
            
            if (canJoin)
                SteamManager.instance.UnlockLobby();
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
            if (PhotonNetwork.IsMasterClient || SemiFunc.RunIsShop() || SemiFunc.RunIsLobby())
                PhotonNetwork.RemoveBufferedRPCs(self.PhotonView.ViewID);
            
            orig.Invoke(self);
        }
        
        private static void PlayerAvatar_StartHook(Action<PlayerAvatar> orig, PlayerAvatar self)
        {
            orig.Invoke(self);
            
            if (!PhotonNetwork.IsMasterClient || !SemiFunc.RunIsShop() && !SemiFunc.RunIsLobby())
                return;
            
            self.photonView.RPC("LoadingLevelAnimationCompletedRPC", RpcTarget.AllBuffered);
        }

        private static void RemoveFromPhotonCache(PhotonView photonView)
        {
            var removeFilter = AccessTools.Field(typeof(PhotonNetwork), "removeFilter").GetValue(null) as Hashtable;
            var keyByteSeven = AccessTools.Field(typeof(PhotonNetwork), "keyByteSeven").GetValue(null);
            var serverCleanOptions = AccessTools.Field(typeof(PhotonNetwork), "ServerCleanOptions").GetValue(null) as RaiseEventOptions;
            var raiseEventInternal = AccessTools.Method(typeof(PhotonNetwork), "RaiseEventInternal");
 
            removeFilter![keyByteSeven] = photonView.InstantiationId;
            serverCleanOptions!.CachingOption = EventCaching.RemoveFromRoomCache;
            raiseEventInternal.Invoke(null, [(byte) 202, removeFilter, serverCleanOptions, SendOptions.SendReliable]);
        }
        
        private void Awake()
        {
            logger.LogDebug("Hooking `RunManager.ChangeLevel`");
            new Hook(AccessTools.Method(typeof(RunManager), "ChangeLevel"), RunManager_ChangeLevelHook);
            
            logger.LogDebug("Hooking `PlayerAvatar.Spawn`");
            new Hook(AccessTools.Method(typeof(PlayerAvatar), "Spawn"), PlayerAvatar_SpawnHook);

            logger.LogDebug("Hooking `LevelGenerator.Start`");
            new Hook(AccessTools.Method(typeof(LevelGenerator), "Start"), LevelGenerator_StartHook);
            
            logger.LogDebug("Hooking `LevelGenerator.Start`");
            new Hook(AccessTools.Method(typeof(LevelGenerator), "Start"), LevelGenerator_StartHook);
            
            logger.LogDebug("Hooking `PlayerAvatar.Start`");
            new Hook(AccessTools.Method(typeof(PlayerAvatar), "Start"), PlayerAvatar_StartHook);
        }
    }
}