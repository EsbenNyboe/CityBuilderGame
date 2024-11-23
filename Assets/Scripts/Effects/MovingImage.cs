using UnityEngine;

public class MovingImage : MonoBehaviour
{
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _offset;

    private void Update()
    {
        var position = transform.position;
        position.y += _moveSpeed * Time.deltaTime;
        transform.position = position;
    }

    private void OnEnable()
    {
        transform.position += new Vector3(0, _offset, 0);
    }
}