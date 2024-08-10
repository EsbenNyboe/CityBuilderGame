using UnityEngine;

public class ShootPopup : MonoBehaviour
{
    private float _destroyTimer = 1f;

    private void Update()
    {
        var moveSpeed = 2f;
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        _destroyTimer -= Time.deltaTime;
        if (_destroyTimer <= 0)
        {
            Destroy(gameObject);
        }
    }
}