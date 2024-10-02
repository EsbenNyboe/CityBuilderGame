using UnityEngine;

class CameraController : MonoBehaviour
{
    [SerializeField] private float _movementSpeed;

    private void Update()
    {
        var moveDelta = Vector3.zero;

        if (Input.GetKey(KeyCode.A))
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
}