// using System.Collections.Generic;
// using System.ComponentModel.DataAnnotations;
// using Mirror;
// using Relay;
// using Snapser;
// using Snapser.Handlers;
// using UnityEngine;
// using UnityEngine.Serialization;
//
// /*
// 	Documentation: https://mirror-networking.gitbook.io/docs/components/network-manager
// 	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
// */
//
// // Custom NetworkManager that simply assigns the correct racket positions when
// // spawning players. The built in RoundRobin spawn method wouldn't work after
// // someone reconnects (both players would be on the same side).
// [AddComponentMenu("")]
// public class SpaceshipNetworkManager : NetworkManager
// {
//     public new static SpaceshipNetworkManager singleton { get; private set; }
//
//     [SerializeField, Required] Transform playerSpawnPoint, lightControllerSpawnPoint;
//     [SerializeField, Required] private List<Color> spaceshipColors;
//     
//     private GameObject lightController;
//     HermesTransport hermesTransport;
//
//     public HermesTransport HermesTransport => hermesTransport;
//     
//     private SnapserManager snapserManager;
//     public SnapserManager SnapserManager => snapserManager;
//     private List<GameObject> players = new List<GameObject>();
//
//     public override void Awake()
//     { 
//         base.Awake();
//         singleton = this;
//         hermesTransport = GetComponent<HermesTransport>();
//         snapserManager = new SnapserManager(networkAddress);
//     }
//     
//     public override void OnServerAddPlayer(NetworkConnectionToClient conn)
//     {
//         //Add players on all clients after the client is started and add to the server
//         int playerToBeCreatedCount = numPlayers + 1;
//         Vector3 startPosition = new Vector3(playerSpawnPoint.position.x, playerSpawnPoint.position.y - (playerToBeCreatedCount - 1) * 8, playerSpawnPoint.position.z);
//         GameObject player = Instantiate(playerPrefab, startPosition, playerSpawnPoint.rotation);
//         players.Add(player);
//         //Spaceship spaceship = player.GetComponent<Spaceship>();
//         //spaceship.InitializeSpaceship(spaceshipColors[numPlayers]);
//         NetworkServer.AddPlayerForConnection(conn, player);
//         Debug.Log("Player " + numPlayers + " of " + hermesTransport.RelayConnectionsCount + " created at location " + player.transform.position);
//         
//         
//         //When all players have been instantiated, instantiated the game light controller for all clients
//         if (numPlayers == hermesTransport.RelayConnectionsCount)
//         {
//             Debug.Log("Creating the game light after " + numPlayers + " of " + hermesTransport.RelayConnectionsCount + " joined and were created.");
//             lightController = Instantiate(spawnPrefabs.Find(prefab => prefab.name == "GameLightController"));
//             Vector3 lightControllerPosition = lightController.transform.position;
//             lightController.transform.position = new Vector3(lightControllerSpawnPoint.position.x,
//                 lightControllerPosition.y, lightControllerPosition.z);
//             NetworkServer.Spawn(lightController);
//         }
//     }
//     
//     public override void OnServerDisconnect(NetworkConnectionToClient conn)
//     {
//         //Destroy the game light controller when server disconnects
//         if (lightController != null)
//             NetworkServer.Destroy(lightController);
//
//         //Call base functionality (actually destroys the player)
//         base.OnServerDisconnect(conn);
//     }
//
//     public Color GetSpaceshipColor(GameObject player)
//     {
//         if (players.Contains(player))
//         {
//             int playerIndex = players.IndexOf(player) % spaceshipColors.Count;
//             return spaceshipColors[playerIndex];
//         }
//         
//         Debug.LogWarning("Game object not found in the player game object lists ");
//         return Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
//     }
// }