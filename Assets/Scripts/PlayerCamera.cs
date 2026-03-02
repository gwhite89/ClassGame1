using UnityEngine;

#region Class Documentation
/// <summary>
/// Professional-grade 3rd person camera for Unity 6.3.
/// Features: Spherical sliding, Hooke's Law springs, dynamic FOV, and Occlusion prevention.
/// </summary>
#endregion

[RequireComponent(typeof(Camera))]
public class PlayerCamera : MonoBehaviour
{
    #region Serialized Fields - Targeting
    [Header("Target Settings")]
    [SerializeField] [Tooltip("The transform to follow. If null, will search for 'Player' tag.")]
    private Transform _target;

    [SerializeField] [Tooltip("The tag used to find the player if no target is assigned.")]
    private string _playerTag = "Player";
    #endregion

    #region Serialized Fields - Positioning
    [Header("Positioning")]
    [SerializeField] [Tooltip("Desired distance the camera maintains from the player.")]
    private float _distanceFromPlayer = 5.0f;

    [SerializeField] [Tooltip("Height offset from the player's pivot point.")]
    private float _heightOffset = 2.0f;

    [SerializeField] [Tooltip("Smoothing factor for following movement (higher is tighter).")]
    private float _followSpeed = 12.0f;

    [SerializeField] [Tooltip("Speed of the camera sliding along the radius during turns.")]
    private float _orbitSpeed = 6.0f;
    #endregion

    #region Serialized Fields - Effects
    [Header("Movement Bob (Walk/Run)")]
    [SerializeField] [Tooltip("Frequency of the walking bob oscillation.")]
    private float _bobFrequency = 4.8f;
    [SerializeField] [Tooltip("Intensity of the walking bob (vertical amplitude).")]
    private float _bobAmount = 0.04f;

    [Header("Impact Spring (Jump/Recoil)")]
    [SerializeField] [Tooltip("Strength of the initial physics kick.")]
    private float _impactStrength = 0.6f;
    [SerializeField] [Tooltip("How quickly the impact bounce settles.")]
    private float _impactDamping = 6.0f;
    [SerializeField] [Tooltip("The stiffness of the spring return.")]
    private float _impactStiffness = 120.0f;

    [Header("Dynamic FOV")]
    [SerializeField] private float _baseFOV = 60f;
    [SerializeField] private float _sprintFOV = 75f;
    [SerializeField] private float _fovLerpSpeed = 4f;
    #endregion

    #region Serialized Fields - Occlusion
    [Header("Occlusion (Wall Clipping)")]
    [SerializeField] [Tooltip("Minimum distance the camera can snap to the player.")]
    private float _minOcclusionDistance = 0.5f;

    [SerializeField] [Tooltip("Layers that block the camera view. DO NOT include the Player layer!")]
    private LayerMask _occlusionLayerMask;

    [SerializeField] [Tooltip("Padding to prevent near-clipping plane artifacts.")]
    private float _raycastPadding = 0.15f;
    #endregion

    #region Private State
    private Camera _cam;
    private Vector3 _currentFollowVelocity;
    private Vector3 _currentOrbitOffset;
    private Vector3 _baseFollowPosition;
    private float _bobTimer;
    private float _impactVerticalOffset;
    private float _impactVelocity;
    private Vector3 _lastTargetPosition;
    private float _currentAdjustedDistance;
    #endregion

    #region Logic
    private void Start()
    {
        _cam = GetComponent<Camera>();

        if (_target == null)
        {
            GameObject playerObj = GameObject.FindWithTag(_playerTag);
            if (playerObj != null) _target = playerObj.transform;
        }

        if (_target != null)
        {
            _lastTargetPosition = _target.position;
            _currentOrbitOffset = -_target.forward * _distanceFromPlayer;
            _baseFollowPosition = _target.position + _currentOrbitOffset + (Vector3.up * _heightOffset);
            _currentAdjustedDistance = _distanceFromPlayer;
        }
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        UpdateBasePosition();
        CheckForOcclusion();

        float bob = CalculateBobbing();
        float spring = UpdateImpactSpring();

        transform.position = _baseFollowPosition + (Vector3.up * (bob + spring));
        transform.LookAt(_target.position + (Vector3.up * (_heightOffset * 0.7f)));
        
        UpdateFOV();
    }

    private void UpdateBasePosition()
    {
        Vector3 desiredOrbitOffset = -_target.forward * _distanceFromPlayer;
        _currentOrbitOffset = Vector3.Slerp(_currentOrbitOffset, desiredOrbitOffset, _orbitSpeed * Time.deltaTime);
        Vector3 targetPos = _target.position + _currentOrbitOffset + (Vector3.up * _heightOffset);
        _baseFollowPosition = Vector3.SmoothDamp(_baseFollowPosition, targetPos, ref _currentFollowVelocity, 1f / _followSpeed);
    }

    private void CheckForOcclusion()
    {
        Vector3 playerLookCenter = _target.position + (Vector3.up * (_heightOffset * 0.7f));
        Vector3 desiredDir = (_baseFollowPosition - playerLookCenter).normalized;
        float castDistance = _distanceFromPlayer + _raycastPadding;

        if (Physics.Raycast(playerLookCenter, desiredDir, out RaycastHit hit, castDistance, _occlusionLayerMask))
        {
            _currentAdjustedDistance = Mathf.Clamp(hit.distance - _raycastPadding, _minOcclusionDistance, _distanceFromPlayer);
        }
        else
        {
            _currentAdjustedDistance = Mathf.Lerp(_currentAdjustedDistance, _distanceFromPlayer, Time.deltaTime * _followSpeed * 0.5f);
        }

        _baseFollowPosition = playerLookCenter + (desiredDir * _currentAdjustedDistance);
    }

    private float CalculateBobbing()
    {
        float moveSpeed = (new Vector3(_target.position.x, 0, _target.position.z) - 
                           new Vector3(_lastTargetPosition.x, 0, _lastTargetPosition.z)).magnitude / Time.deltaTime;
        _lastTargetPosition = _target.position;

        if (moveSpeed > 0.1f)
        {
            _bobTimer += Time.deltaTime * _bobFrequency;
            return Mathf.Sin(_bobTimer) * _bobAmount;
        }
        _bobTimer = 0;
        return 0;
    }

    private float UpdateImpactSpring()
    {
        float springForce = -_impactStiffness * _impactVerticalOffset;
        float dampingForce = _impactDamping * _impactVelocity;
        _impactVelocity += (springForce - dampingForce) * Time.deltaTime;
        _impactVerticalOffset += _impactVelocity * Time.deltaTime;
        return _impactVerticalOffset;
    }

    private void UpdateFOV()
    {
        if (_cam == null) return;
        float currentGroundSpeed = (new Vector3(_target.position.x, 0, _target.position.z) - new Vector3(_lastTargetPosition.x, 0, _lastTargetPosition.z)).magnitude / Time.deltaTime;
        bool isSprinting = currentGroundSpeed > 6.0f; // Threshold for sprint FOV
        float targetFOV = isSprinting ? _sprintFOV : _baseFOV;
        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV, Time.deltaTime * _fovLerpSpeed);
    }

    public void TriggerImpactBounce() => _impactVelocity += _impactStrength;
    #endregion
}