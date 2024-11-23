using UnityEngine;

public class MovingImage : MonoBehaviour
{
    [SerializeField] private float _moveSpeed;

    private void Update()
    {
        var position = transform.position;
        position.y += _moveSpeed * Time.deltaTime;
        transform.position = position;
    }
}