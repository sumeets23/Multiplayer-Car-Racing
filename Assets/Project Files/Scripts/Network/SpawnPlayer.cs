using System.Collections.Generic;
using Car.WheelsManagement;
using Photon.Pun;
using RaceManagement;
using TMPro;
using UnityEngine;

namespace Network
{
    public class SpawnPlayer : MonoBehaviourPunCallbacks
    {
        /// <summary>
        /// List of 4 car prefabs — index 0 = colour 1, index 1 = colour 2, etc.
        /// Assign them in the Inspector in the same order as the colour buttons in the lobby.
        /// Each prefab MUST be inside a Resources folder so PhotonNetwork.Instantiate can find it.
        /// </summary>
        [SerializeField] private List<GameObject> playerPrefabs = new List<GameObject>();

        [SerializeField] private Transform[] spawnPositions;
        private int _numberPlayers;

        [SerializeField] private TextMeshProUGUI numberOfPlayersInRace;
        [SerializeField] private RaceController raceController;
        [SerializeField] private List<GameObject> canvasObjects = new List<GameObject>();
        [SerializeField] public List<Material> colors = new List<Material>();

        private GameObject spawnedCar;

        private void Start()
        {
            CheckPlayers();
            SpawnNewPlayer();
        }

        private void Update()
        {
            var numberOfPlayersInScene = FindObjectsOfType<CarMovementController>();
            numberOfPlayersInRace.text = numberOfPlayersInScene.Length + "/" + PhotonNetwork.CurrentRoom.PlayerCount;
            if (numberOfPlayersInScene.Length == PhotonNetwork.CurrentRoom.PlayerCount)
            {
                if (raceController.enabled == false)
                {
                    GetComponent<PhotonView>().RPC("RaceStart", RpcTarget.AllBuffered, null);
                }
            }
        }

        private void CheckPlayers()
        {
            _numberPlayers = PhotonNetwork.CountOfPlayersInRooms;
            for (var i = 0; i <= _numberPlayers; i++)
            {
                if (_numberPlayers > 4)
                {
                    _numberPlayers -= 4;
                }
            }
        }

        private void SpawnNewPlayer()
        {
            // Read the colour chosen in the lobby (1–4).
            // It is stored as a Photon CustomProperty under the key "color".
            int colorIndex = 0; // default to first prefab if nothing was chosen
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("color", out object colorValue))
            {
                // color is stored as 1-based (1,2,3,4) → convert to 0-based index
                colorIndex = Mathf.Clamp((int)colorValue - 1, 0, playerPrefabs.Count - 1);
            }

            // Guard: make sure the list is populated
            if (playerPrefabs.Count == 0)
            {
                Debug.LogError("[SpawnPlayer] playerPrefabs list is empty! Please assign the 4 car prefabs in the Inspector.");
                return;
            }

            GameObject selectedPrefab = playerPrefabs[colorIndex];

            var spawnPos   = spawnPositions[PhotonNetwork.LocalPlayer.ActorNumber - 1].position;
            var spawnRot   = spawnPositions[PhotonNetwork.LocalPlayer.ActorNumber - 1].rotation;

            spawnedCar = PhotonNetwork.Instantiate(selectedPrefab.name, spawnPos, spawnRot, 0);
            _numberPlayers++;
        }

        [PunRPC]
        public void RaceStart()
        {
            raceController.enabled = true;
        }
    }
}
