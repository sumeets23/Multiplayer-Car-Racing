using System;
using System.Collections;
using System.Collections.Generic;
using Car.WheelsManagement;
using Photon.Pun;
using Photon.Realtime;
using RaceManagement;
using TMPro;
using UnityEngine;
using PhotonNetwork = Photon.Pun.PhotonNetwork;


namespace Network
{
    ///<summary>
    /// This class synchronizes players between each other. Players send to one another information about themselves, like their color, name, and position. For that we used streams to send information to another players.
    ///</summary>
    public class Sync : Photon.Pun.MonoBehaviourPun, IPunObservable
    {
        private Vector3 _trueLoc;
        private Quaternion _trueRot;
        private PhotonView _photonView;
        private SpawnPlayer _spawnPlayer;
        private int _color;
        private string _name;
        [SerializeField] private GameObject body;
        [SerializeField] private RaceParticipant raceParticipant;
        [SerializeField] private TextMeshProUGUI text;

        // Caching for performance
        private MeshRenderer _bodyMeshRenderer;
        private bool _isInitialized = false;

        // Advanced smooth interpolation variables
        private Vector3 _networkPosition;
        private Quaternion _networkRotation;
        private float _fraction;

        private void Start()
        {
            _photonView = GetComponent<PhotonView>();
            _spawnPlayer = FindObjectOfType<SpawnPlayer>();
            
            // Wait for initialization from network on remote clients
            if (_photonView.IsMine)
            {
                if (gameObject.TryGetComponent(out CarMovementController _))
                {
                    body.GetComponent<MeshRenderer>().material =
                        _spawnPlayer.colors[(int) PhotonNetwork.LocalPlayer.CustomProperties["color"] - 1];
                }
                
                if (raceParticipant != null)
                {
                    raceParticipant.Name = (string) PhotonNetwork.LocalPlayer.CustomProperties["name"];
                }
            }
            else
            {
                if (gameObject.TryGetComponent(out CarMovementController _))
                {
                    _bodyMeshRenderer = transform.Find("View")?.transform.Find("body")?.GetComponent<MeshRenderer>();
                }
                
                // CRITICAL FIX: Disable physics simulation for remote vehicles
                // Otherwise the physics engine fights the network transform position & gravity pulls the car down creating extreme stutter
                if (TryGetComponent(out Rigidbody rb))
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
                
                // CRITICAL FIX 2: Disable WheelColliders entirely on remote cars!
                // Since the rigid body is kinematic, WheelColliders will force their suspension calculation to 0, which pushes 
                // the graphical wheel meshes UP inside the fenders (mesh overlapping glitch).
                WheelCollider[] wheelColliders = GetComponentsInChildren<WheelCollider>();
                foreach (WheelCollider wc in wheelColliders)
                {
                    wc.enabled = false;
                }
                
                _networkPosition = transform.position;
                _networkRotation = transform.rotation;
                _trueLoc = transform.position;
                _trueRot = transform.rotation;
            }
        }

        private void Update()
        {
            if (!_photonView.IsMine)
            {
                // Exponential decay interpolation natively prevents any overshooting ("going forward then stopping/coming back")
                // because it ALWAYS converges strictly towards the most recent _trueLoc without predicting forward.
                // A speed of 15f keeps the car highly responsive to sudden speed changes while smoothing out dropped packets.
                transform.position = Vector3.Lerp(transform.position, _trueLoc, Time.deltaTime * 15f);
                transform.rotation = Quaternion.Slerp(transform.rotation, _trueRot, Time.deltaTime * 15f);
                
                // Only update cosmetics once when the data is first received, not every frame
                if (!_isInitialized && _color != 0 && !string.IsNullOrEmpty(_name))
                {
                    if (_bodyMeshRenderer != null && _spawnPlayer != null)
                    {
                        _bodyMeshRenderer.material = _spawnPlayer.colors[_color - 1];
                    }
                    if (raceParticipant != null) raceParticipant.Name = _name;
                    if (text != null) text.text = _name;
                    
                    _isInitialized = true;
                }
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsReading)
            {
                if (!_photonView.IsMine)
                {
                    this._trueLoc = (Vector3) stream.ReceiveNext();
                    this._trueRot = (Quaternion) stream.ReceiveNext();
                    
                    // We receive these every network tick but we only apply them once using _isInitialized
                    this._color = (int) stream.ReceiveNext();
                    this._name = (string) stream.ReceiveNext();
                }
            }
            else
            {
                if (_photonView.IsMine)
                {
                    stream.SendNext(transform.position);
                    stream.SendNext(transform.rotation);
                    stream.SendNext((int) PhotonNetwork.LocalPlayer.CustomProperties["color"]);
                    stream.SendNext((string)PhotonNetwork.LocalPlayer.CustomProperties["name"]);
                }
            }
        }
    }
}
