using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private PlayerData _playerData;
    private Transform _player;
    private float _MouseX { get { return Input.GetAxis("Mouse X"); } }
    private float _MouseY { get { return Input.GetAxis("Mouse Y"); } }
    private Transform _cameraHolder;
    private Transform _yaw;
    private Transform _pitch;


    private void Awake()
    {
        _player = GameObject.FindGameObjectWithTag("Player").transform;
        _playerData = _player.GetComponent<PlayerController>().PlayerData;
        Cursor.lockState = CursorLockMode.Locked;
        _cameraHolder = transform.parent.parent.parent;
        _yaw = transform.parent;
        _pitch = transform.parent.parent;
    }

    // Update is called once per frame
    void Update()
    {
        _cameraHolder.position = _player.position;
        _yaw.Rotate(_playerData.CameraRotationSpeed * Time.deltaTime * -_MouseY * Vector3.right);
        _pitch.Rotate(_playerData.CameraRotationSpeed * Time.deltaTime * _MouseX * Vector3.up);
    }
}
