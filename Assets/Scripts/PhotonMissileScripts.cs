using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotonMissileScript : MonoBehaviour
{
    private PhotonGameManager photonGameManager;

    void Start()
    {
        photonGameManager = GameObject.FindObjectOfType<PhotonGameManager>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Logic đã được xử lý trong RPC, chỉ cần hủy tên lửa
        Destroy(gameObject);
    }
}