using System;
using UnityEngine;

class CameraController : MonoBehaviour
{
    [SerializeField] private float _movementSpeed;
    [SerializeField] private float _zoomSpeed;
    [SerializeField] private float _minSize;
    [SerializeField] private float _maxSize;

    private void Update()
    {
        CameraMovement();
        CameraZoom();
    }

    private void CameraMovement()
    {
        var moveDelta = Vector3.zero;

        if (Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.LeftControl))
        {
            moveDelta.x -= _movementSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveDelta.x += _movementSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveDelta.y -= _movementSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.W))
        {
            moveDelta.y += _movementSpeed * Time.deltaTime;
        }

        transform.position += moveDelta;
    }

    private void CameraZoom()
    {
        var size = Camera.main.orthographicSize;
        size -= Input.GetAxis("Mouse ScrollWheel") * _zoomSpeed;
        size = Mathf.Clamp(size, _minSize, _maxSize);
        Camera.main.orthographicSize = size;
    }
}