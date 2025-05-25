using UnityEngine;

namespace Rendering
{
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance;

        [HideInInspector] public Vector3 FollowPosition;
        [HideInInspector] public float FollowZoomAmount;

        [SerializeField] private float _movementSpeed;
        [SerializeField] private float _zoomSpeed;

        [Range(0.00001f, 0.99999f)] [SerializeField]
        private float _zoomAcceleration = 0.067f;

        [SerializeField] private float _minSize;
        [SerializeField] private float _maxSize;
        [SerializeField] private float _minSizeListenerProximity;
        [SerializeField] private float _maxSizeListenerProximity;

        [Range(0.00001f, 0.99999f)] [SerializeField]
        private float _followLerpFactor;

        private bool _isFollowingSelectedUnit;
        private bool _isZoomingOnSelectedUnits;

        private float _zoomMomentum;

        private void Awake()
        {
            Instance = this;
        }

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

            if (Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.LeftControl))
            {
                moveDelta.y -= _movementSpeed * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.W))
            {
                moveDelta.y += _movementSpeed * Time.deltaTime;
            }

            transform.position += moveDelta;

            _isZoomingOnSelectedUnits = Input.GetKeyDown(KeyCode.LeftShift) ? !_isZoomingOnSelectedUnits : _isZoomingOnSelectedUnits;

            if (Input.GetKeyDown(KeyCode.Return))
            {
                _isFollowingSelectedUnit = !_isFollowingSelectedUnit;
            }

            if (_isFollowingSelectedUnit && FollowPosition != Vector3.zero)
            {
                var followPosition = new Vector3(FollowPosition.x, FollowPosition.y, transform.position.z);
                transform.position = Vector3.Lerp(transform.position, followPosition, _followLerpFactor);
            }
        }

        private void CameraZoom()
        {
            var scrollAmount = Input.GetAxis("Mouse ScrollWheel");
            if (Input.GetKey(KeyCode.LeftControl))
            {
                _zoomMomentum += scrollAmount;
                scrollAmount = _zoomMomentum;
            }

            _zoomMomentum = Mathf.Lerp(_zoomMomentum, 0, _zoomAcceleration);

            var mouseScreenPos = Input.mousePosition;
            var mouseWorldBefore = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0));

            var size = Camera.main.orthographicSize;
            if (_isZoomingOnSelectedUnits)
            {
                size += FollowZoomAmount * _zoomSpeed;
            }

            size -= scrollAmount * _zoomSpeed;
            size = Mathf.Clamp(size, _minSize, _maxSize);
            Camera.main.orthographicSize = size;

            var mouseWorldAfter = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0));
            var delta = mouseWorldBefore - mouseWorldAfter;
            transform.position += delta;

            var zoomAmount = (size - _minSize) / _maxSize;
            var listenerProximity = Mathf.Lerp(_minSizeListenerProximity, _maxSizeListenerProximity, zoomAmount);

            transform.position = new Vector3(transform.position.x, transform.position.y, listenerProximity);
        }

        public void SetMaxSize(float maxSize)
        {
            _maxSize = maxSize;
        }
    }
}