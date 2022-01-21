using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JamKit
{
    /// <summary>
    /// Q3-based first person controller
    /// </summary>
    public class PlayerMotor : MonoBehaviour
    {
        #region Drag Drop

        [SerializeField] private Transform _camTransform = default;

        // Collision resolving is done with respect to this volume
        [SerializeField] private CapsuleCollider _collisionVolume = default;

        // Collision will not happend with these layers
        // One of them has to be this controller's own layer
        [SerializeField] private LayerMask _excludedLayers = default;

        [SerializeField] private Footsteps _footsteps = default;

        [SerializeField] private bool _debugInfo = false;

        [SerializeField] private List<Transform> _groundedRayPositions = default;

        #endregion

        #region Configuration

        [Header("Config")]

        // The controller can collide with colliders within this radius
        [SerializeField]
        private float _radius = 2f;

        // Ad-hoc approach to make the controller accelerate faster
        [SerializeField] private float _groundAccelerationCoeff = 10.0f;

        // How fast the controller accelerates while it's not grounded
        [SerializeField] private float _airAccelCoeff = 1f;

        // Air deceleration occurs when the player gives an input that's not aligned with the current velocity
        [SerializeField] private float _airDecelCoeff = 1.5f;

        // Along a dimension, we can't go faster than this
        // This dimension is relative to the controller, not global
        // Meaning that "max speend along X" means "max speed along 'right side' of the controller"
        [SerializeField] private float _maxSpeedAlongOneDimension = 8f;

        // When pressing to shift
        [SerializeField] private float _walkSpeed = 4f;

        // How fast the controller decelerates on the grounded
        [SerializeField] private float _friction = 15;

        // Stop if under this speed
        [SerializeField] private float _frictionSpeedThreshold = 0.5f;

        // Push force given when jumping
        [SerializeField] private float _jumpStrength = 8f;

        // yeah...
        [SerializeField] private float _gravityAmount = 24f;

        // How precise the controller can change direction while not grounded 
        [SerializeField] private float _airControlPrecision = 16f;

        // When moving only forward, increase air control dramatically
        [SerializeField] private float _airControlAdditionForward = 16f;

        [SerializeField] private float _noClipSpeed = 50f;

        [SerializeField] private bool _jumpEnabled = true;

        #endregion

        #region Fields

        // The real velocity of this controller
        public Vector3 Velocity
        {
            get { return _velocity; }
        }

        private Vector3 _velocity;

        public bool IsWalking { get; private set; }

        // Caching...
        private readonly Collider[] _overlappingColliders = new Collider[10]; // Hope no more is needed
        private Transform _ghostJumpRayPosition;

        // Some information to persist
        private bool _isGroundedInPrevFrame = false;
        private bool _isGonnaJump = false;
        private Vector3 _wishDirDebug;

        private bool _noClipMove = false;

        #endregion

        private void Start()
        {
            _ghostJumpRayPosition = _groundedRayPositions.Last();
        }

#if UNITY_EDITOR
        // Only for debug drawing
        private void OnGUI()
        {
            if (!_debugInfo)
            {
                return;
            }

            // Print current horizontal speed
            var ups = _velocity;
            ups.y = 0;
            GUI.Box(new Rect(Screen.width / 2f - 50, Screen.height / 2f + 50, 100, 40),
                (Mathf.Round(ups.magnitude * 100) / 100).ToString());

            // Draw horizontal speed as a line
            var mid = new Vector2(Screen.width / 2, Screen.height / 2); // Should remain integer division, otherwise GUI drawing gets screwed up
            var v = _camTransform.InverseTransformDirectionHorizontal(_velocity) * _velocity.WithY(0).magnitude * 10f;
            if (v.WithY(0).magnitude > 0.0001)
            {
                Drawing.DrawLine(mid, mid + Vector2.up * -v.z + Vector2.right * v.x, Color.red, 3f);
            }

            // Draw input direction
            var w = _camTransform.InverseTransformDirectionHorizontal(_wishDirDebug) * 100;
            if (w.magnitude > 0.001)
            {
                Drawing.DrawLine(mid, mid + Vector2.up * -w.z + Vector2.right * w.x, Color.blue, 2f);
            }
        }
#endif

        public void Tick(float dt)
        {
            Cursor.lockState = CursorLockMode.Locked; // Keep doing this. We don't want cursor anywhere just yet

            // We use GetAxisRaw, since we need it to feel as responsive as possible
            Vector3 moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

            #region No clip

#if CHEATS_ENABLED
        if (Input.GetKeyDown(KeyCode.RightAlt))
        {
            _noClipMove = !_noClipMove;
        }

        if (Input.GetMouseButtonDown(2))
        {
            _velocity = _velocity.normalized * 20;
        }
#endif
            if (_noClipMove)
            {
                float y = 0;
                if (Input.GetKey(KeyCode.Space))
                {
                    y = 1;
                }
                else if (Input.GetKey(KeyCode.LeftControl))
                {
                    y = -1;
                }

                moveInput.y = y;
                transform.position += _camTransform.TransformDirection(moveInput) * _noClipSpeed * Time.deltaTime;
                _velocity = Vector3.zero;

                return;
            }

            #endregion

            IsWalking = Input.GetKey(KeyCode.LeftShift);

            if (_jumpEnabled)
            {
                if (Input.GetKeyDown(KeyCode.Space) && !_isGonnaJump)
                {
                    _isGonnaJump = true;
                }
                else if (Input.GetKeyUp(KeyCode.Space))
                {
                    _isGonnaJump = false;
                }
            }

            // MOVEMENT
            var wishDir = _camTransform.TransformDirectionHorizontal(moveInput); // We want to go in this direction
            _wishDirDebug = wishDir.ToHorizontal();
            var isGrounded = IsGrounded(out Vector3 groundNormal);

            if (isGrounded) // Ground move
            {
                if (_isGroundedInPrevFrame && !_isGonnaJump) // Don't apply friction if just landed or about to jump
                {
                    ApplyFriction(ref _velocity, dt);
                }

                var speedLimit = IsWalking ? _walkSpeed : _maxSpeedAlongOneDimension;
                Accelerate(ref _velocity, wishDir, _groundAccelerationCoeff, speedLimit, dt);

                // Crop up horizontal velocity component
                _velocity = Vector3.ProjectOnPlane(_velocity, groundNormal);
                if (_isGonnaJump)
                {
                    // Jump away
                    _velocity += Gravity.Up * _jumpStrength;
                    Sfx.Instance.Play("Jump");
                }
            }
            else // Air move
            {
                // If the input doesn't have the same facing with the current velocity
                // then slow down instead of speeding up
                var coeff = Vector3.Dot(_velocity, wishDir) > 0 ? _airAccelCoeff : _airDecelCoeff;

                Accelerate(ref _velocity, wishDir, coeff, _maxSpeedAlongOneDimension, dt);

                if (Mathf.Abs(moveInput.z) > 0.0001) // Pure side velocity doesn't allow air control
                {
                    ApplyAirControl(ref _velocity, wishDir, moveInput, dt);
                }

                _velocity += Gravity.Down * (_gravityAmount * dt);
            }

            var displacement = _velocity * dt;

            // If we're moving too fast, make sure we don't hollow through any collider
            if (displacement.magnitude > _collisionVolume.radius)
            {
                ClampDisplacement(ref _velocity, ref displacement, transform.position);
            }

            transform.position += displacement;

            var collisionDisplacement = ResolveCollisions(ref _velocity);

            transform.position += collisionDisplacement;

            _footsteps.Tick(isGrounded, _velocity, (displacement + collisionDisplacement).magnitude);

            _isGroundedInPrevFrame = isGrounded;
        }

        private void Accelerate(ref Vector3 playerVelocity, Vector3 accelDir, float accelCoeff, float speedLimit, float dt)
        {
            // How much speed we already have in the direction we want to speed up
            var projSpeed = Vector3.Dot(playerVelocity, accelDir);

            // How much speed we need to add (in that direction) to reach max speed
            var addSpeed = speedLimit - projSpeed;
            if (addSpeed <= 0)
            {
                return;
            }

            // How much we are gonna increase our speed
            // maxSpeed * dt => the real deal. a = v / t
            // accelCoeff => ad hoc approach to make it feel better
            var accelAmount = accelCoeff * _maxSpeedAlongOneDimension * dt;

            // If we are accelerating more than in a way that we exceed maxSpeedInOneDimension, crop it to max
            if (accelAmount > addSpeed)
            {
                accelAmount = addSpeed;
            }

            playerVelocity += accelDir * accelAmount; // Magic happens here
        }

        private void ApplyFriction(ref Vector3 playerVelocity, float dt)
        {
            var speed = playerVelocity.magnitude;
            if (speed <= 0.00001)
            {
                return;
            }

            var downLimit = Mathf.Max(speed, _frictionSpeedThreshold); // Don't drop below treshold
            var dropAmount = speed - (downLimit * _friction * dt);
            if (dropAmount < 0)
            {
                dropAmount = 0;
            }

            playerVelocity *= dropAmount / speed; // Reduce the velocity by a certain percent
        }

        private void ApplyAirControl(ref Vector3 playerVelocity, Vector3 accelDir, Vector3 moveInput, float dt)
        {
            // This only happens in the horizontal plane
            // TODO: Verify that these work with various gravity values
            var playerDirHorz = playerVelocity.ToHorizontal().normalized;
            var playerSpeedHorz = playerVelocity.ToHorizontal().magnitude;

            var dot = Vector3.Dot(playerDirHorz, accelDir);
            if (dot > 0)
            {
                var k = _airControlPrecision * dot * dot * dt;

                // CPMA thingy:
                // If we want pure forward movement, we have much more air control
                var isPureForward = Mathf.Abs(moveInput.x) < 0.0001 && Mathf.Abs(moveInput.z) > 0;
                if (isPureForward)
                {
                    k *= _airControlAdditionForward;
                }

                // A little bit closer to accelDir
                playerDirHorz = playerDirHorz * playerSpeedHorz + accelDir * k;
                playerDirHorz.Normalize();

                // Assign new direction, without touching the vertical speed
                playerVelocity = (playerDirHorz * playerSpeedHorz).ToHorizontal() + Gravity.Up * playerVelocity.VerticalComponent();
            }
        }

        // Calculates the displacement required in order not to be in a world collider
        private Vector3 ResolveCollisions(ref Vector3 playerVelocity)
        {
            // Get nearby colliders
            Physics.OverlapSphereNonAlloc(transform.position,
                _radius + 0.1f,
                _overlappingColliders,
                ~_excludedLayers);

            // NOTE: Total displacement and total penetration are two different things:
            // Penetration is the result of the physics calculation
            // For displacement, we ignore very small penetration results, to make the collision resolving less shaky
            // But land SFX logic needs the uncropped values, because:
            // There are cases where the motor is grounded (due to the raycast check) but doesn't have 
            // vertical penetration resolving, due to the cropping above
            // Therefore SFX logic thinks there's no penetration even when it's grounded
            var totalDisplacement = Vector3.zero;
            var totalPenetration = Vector3.zero;

            var checkedColliderIndices = new HashSet<int>();

            Vector3 velocityBeforeResolve = playerVelocity;

            // If the player is intersecting with that environment collider, separate them
            for (var i = 0; i < _overlappingColliders.Length; i++)
            {
                // Two player colliders shouldn't resolve collision with the same environment collider
                if (checkedColliderIndices.Contains(i))
                {
                    continue;
                }

                var envColl = _overlappingColliders[i];

                // Skip empty slots
                if (envColl == null)
                {
                    continue;
                }

                if (Physics.ComputePenetration(
                    _collisionVolume,
                    _collisionVolume.transform.position,
                    _collisionVolume.transform.rotation,
                    envColl,
                    envColl.transform.position,
                    envColl.transform.rotation,
                    out Vector3 penetrationNormal,
                    out float penetrationDistance))
                {
                    totalPenetration += penetrationNormal * penetrationDistance;

                    // Ignore very small penetrations
                    // Required for standing still on slopes
                    // ... still far from perfect though
                    const float ignoredPenetrationDistanceThreshold = 0.015f;
                    if (penetrationDistance < ignoredPenetrationDistanceThreshold)
                    {
                        continue;
                    }

                    checkedColliderIndices.Add(i);

                    // Get outta that collider!
                    totalDisplacement += penetrationNormal * penetrationDistance;

                    // Crop down the velocity component which is in the direction of penetration
                    playerVelocity -= Vector3.Project(playerVelocity, penetrationNormal);
                }
            }

            // It's better to be in a clean state in the next resolve call
            for (var i = 0; i < _overlappingColliders.Length; i++)
            {
                _overlappingColliders[i] = null;
            }

            _footsteps.HandleLandSfx(totalPenetration, velocityBeforeResolve);

            return totalDisplacement;
        }

        // If one of the rays hit, we're considered to be grounded
        private bool IsGrounded(out Vector3 groundNormal)
        {
            groundNormal = Gravity.Up;

            bool isGrounded = false;
            foreach (var t in _groundedRayPositions)
            {
                // The last one is reserved for ghost jumps
                // Don't check that one if already on the ground
                if (t == _ghostJumpRayPosition && isGrounded)
                {
                    continue;
                }

                RaycastHit hit;
                if (Physics.Raycast(t.position, Gravity.Down, out hit, 0.51f, ~_excludedLayers))
                {
                    groundNormal = hit.normal;
                    isGrounded = true;
                }
            }

            bool tooSteep = Vector3.Dot(groundNormal, Gravity.Up) < 0.6f;
            return isGrounded && !tooSteep;
        }

        // If there's something between the current position and the next, clamp displacement
        private void ClampDisplacement(ref Vector3 playerVelocity, ref Vector3 displacement, Vector3 playerPosition)
        {
            RaycastHit hit;
            if (Physics.Raycast(playerPosition, playerVelocity.normalized, out hit, displacement.magnitude, ~_excludedLayers))
            {
                displacement = hit.point - playerPosition;
            }
        }

        public void ResetAt(Transform t)
        {
            transform.position = t.position;
            _velocity = Vector3.zero;
        }
    }
}