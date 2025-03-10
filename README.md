# LateJoin
Have you ever played R.E.P.O. only to be faced with a friend who wants to join after you've started the lobby? Fret not!

## How can players late join currently?
### Truck (Lobby after the Shop)
The master client will reopen the room after you leave the shop, during the intermission where you are in the truck. Players should be able to join normally, and this mod should support other mods like [MorePlayers](https://thunderstore.io/c/repo/p/zelofi/MorePlayers/) and [REPO ServerList](https://thunderstore.io/c/repo/p/QERT2002/REPO_ServerList/) out of the box.

###  Future plans
We'll try to figure out if we can allow players to join in the shop, (and potentially even during active games, where you would end up as a spectator rather than a player). Theoretically in the case of loading the shop, loading into any level should be possible in that case, but can't be promised and if added, would need to be an experimental feature.

## How does it work?
The game treats the host as the "master client" which dictates players and how they are spawned. Because of this, we are able to change the level for the late joiner, by utilising the buffered RPCs that are provided by Photon PUN. However, doing this requires some fairly major code changes.

**Therefore, be warned, this mod may not be fully compatible with other mods, especially ones that rely on important data that is handled by the client, rather than the master client!**


## Credits
- **nickklmao** for developing the mod
- **felinusfish** for testing and debugging
- **CharlesE2** for help with development
- **R.E.P.O. Modding Server** for the moral support!
