using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using Photon.Pun;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        private static void PlayerAvatar_SpawnHook(Action<PlayerAvatar, Vector3, Quaternion> orig, PlayerAvatar self, Vector3 position, Quaternion rotation)
        {
            if ((bool) AccessTools.Field(typeof(PlayerAvatar), "spawned").GetValue(self))
                return;
            
            orig.Invoke(self, position, rotation);
        }

        private static async void SteamManager_OnGameLobbyJoinRequestedHook(SteamManager self, Lobby _lobby, SteamId _steamID)
        {
            var currentLobby = (Lobby) AccessTools.Field(typeof(SteamManager), "currentLobby").GetValue(self);
            
            if (_lobby.Id == currentLobby.Id)
            {
                Debug.Log("Steam: Already in this lobby.");
            }
            else
            {
                Debug.Log("Steam: Game lobby join requested: " + _lobby.Id);
                
                await SteamMatchmaking.JoinLobbyAsync(_lobby.Id);
                
                AccessTools.Field(typeof(RunManager), "lobbyJoin").SetValue(RunManager.instance, true);
                RunManager.instance.ChangeLevel(true, false);
                
                AccessTools.Field(typeof(SteamManager), "joinLobby").SetValue(self, true);
            }
        }
        
        private void Awake()
        {
            new Hook(AccessTools.Method(typeof(RunManager), "ChangeLevel"), RunManager_ChangeLevelHook);
            new Hook(AccessTools.Method(typeof(PlayerAvatar), "Spawn"), PlayerAvatar_SpawnHook);
            new Hook(AccessTools.Method(typeof(SteamManager), "OnGameLobbyJoinRequested"), SteamManager_OnGameLobbyJoinRequestedHook);
        }
    }
}