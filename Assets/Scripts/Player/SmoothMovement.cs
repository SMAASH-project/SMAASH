using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
 
[RequireComponent(typeof(Rigidbody))]
public class SmoothMovement : MonoBehaviourPun, IPunObservable
{
    private Rigidbody2D _rigidbody;

    private Vector2 _netPosition;
    //private Quaternion _netRotation;
    private Vector2 _previousPosition;

    public bool teleportIfFar;
    public float teleportIfFarDistance;

    [Header ("Lerping [Experimental")]
    
    public float smoothPos = 5.0f;
    public float smoothRot = 5.0f;
    private void Awake()
    {
        PhotonNetwork.SendRate = 30;
        PhotonNetwork.SerializationRate = 10;

        _rigidbody = gameObject.GetComponent<Rigidbody2D>();
    }
   
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(_rigidbody.position);
            //stream.SendNext(_rigidbody.rotation);
            stream.SendNext(_rigidbody.velocity);
        }
        else
        {
            _netPosition = (Vector2)stream.ReceiveNext();
            //_netRotation = (Quaternion)stream.ReceiveNext();
            _rigidbody.velocity = (Vector2)stream.ReceiveNext();
           
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            _netPosition = (_rigidbody.position * lag) + _netPosition;
        }
    }
   
    void FixedUpdate()
    {

        if(photonView.IsMine) return;

        _rigidbody.position = Vector2.Lerp(_rigidbody.position, _netPosition, smoothPos * Time.fixedDeltaTime);
        //_rigidbody.rotation = Vector3.Lerp(_rigidbody.rotation, _netPosition, smoothPos * Time.fixedDeltaTime);

        if(Vector2.Distance(_rigidbody.position, _netPosition) > teleportIfFarDistance)
        {
            _rigidbody.position = _netPosition;
        }
    
    }
}
 
