using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : PortalTraveller
{
	private const float GROUNDED_DIST = 0.3f;

	[Header("Jump")]
	[SerializeField] private float _timeToJumpApex = 0.4f;
	[SerializeField] private float _jumpHeight = 2.5f;

	[Header("Movements")]
	[SerializeField] private float _walkSpeed = 7.5f;
	[SerializeField] private float _runSpeed = 12.5f;
	[SerializeField] private float _maxVelocityY = 15f;

	[Header("Ground")]
	[SerializeField] private LayerMask _groundLayerMask = 0;
	[SerializeField] private float _groundCheckRadius = 0.5f;

	[Header("Rotation")]
	[SerializeField] private float _mouseSensitivity = 400f;

	// Jump
	private float _gravity;
	private float _jumpVelocity;

	// Ground
	private Vector3 _groundNormal = Vector3.up;
	private float _distToGround;
	private bool _isGrounded;

	// Movement
	Vector3 _velocityGround = Vector3.zero;
	Vector3 _velocityAir = Vector3.zero;

	// Components
	private CharacterController _controller;

	// Camera
	private Camera _playerCamera;
	private float _cameraPitch;

	//////////////////////////////////////////////////////////////////////
	protected override void Awake()
	{
		base.Awake();

		_controller = GetComponent<CharacterController>();
		_playerCamera = transform.GetComponentInChildren<Camera>();
	}

	//////////////////////////////////////////////////////////////////////
	protected override void Start()
	{
		base.Start();

		CalculateAerialSettings();

		Cursor.lockState = CursorLockMode.Locked;
	}

	//////////////////////////////////////////////////////////////////////
	void Update()
	{
		CheckForGround();

		Move();
		Rotate();
	}

	//////////////////////////////////////////////////////////////////////
	private void CheckForGround()
	{
		_groundNormal = Vector3.up;
		_distToGround = float.PositiveInfinity;

		RaycastHit hitInfo1;
		if (Physics.SphereCast(transform.position + Vector3.up * (_groundCheckRadius - (_controller.height / 2f)), _groundCheckRadius, Vector3.down, out hitInfo1, _groundCheckRadius, _groundLayerMask))
		{
			_distToGround = (_controller.ClosestPoint(hitInfo1.point) - hitInfo1.point).magnitude;

			float slopeAngle = Vector3.Angle(Vector3.up, hitInfo1.normal);
			if (slopeAngle <= _controller.slopeLimit)
			{
				_groundNormal = hitInfo1.normal;
			}
		}

		_isGrounded = _distToGround < GROUNDED_DIST;
	}

	//////////////////////////////////////////////////////////////////////
	private void Move()
	{
		float inputX = Input.GetAxis("Horizontal");
		float inputZ = Input.GetAxis("Vertical");

		_velocityGround = Vector3.ClampMagnitude(transform.right * inputX + transform.forward * inputZ, 1f);

		bool isSprinting = Input.GetButton("Sprint") && Vector3.Dot(_velocityGround, transform.forward) > 0;

		float movementSpeed = (isSprinting) ? _runSpeed : _walkSpeed;
		_velocityGround *= movementSpeed;

		if (_isGrounded)
		{
			Quaternion slopeQuatOffset = Quaternion.FromToRotation(Vector3.up, _groundNormal);
			_velocityGround = slopeQuatOffset * _velocityGround;

			_velocityAir.y = Mathf.Max(_velocityAir.y, 0f);

			bool shouldJump = Input.GetButtonDown("Jump");
			if (shouldJump)
			{
				_velocityAir.y = _jumpVelocity;
			}
		}

		_velocityAir.y += _gravity * Time.deltaTime;
		_velocityAir.y = Mathf.Clamp(_velocityAir.y, -_maxVelocityY, _maxVelocityY);

		Vector3 move = (_velocityGround + _velocityAir) * Time.deltaTime;
		_controller.Move(move);
	}

	//////////////////////////////////////////////////////////////////////
	private void Rotate()
	{
		float heading = Input.GetAxis("Mouse X") * _mouseSensitivity * Time.deltaTime;
		float pitch = Input.GetAxis("Mouse Y") * _mouseSensitivity * Time.deltaTime;

		_cameraPitch -= pitch;
		_cameraPitch = Mathf.Clamp(_cameraPitch, -90f, 90f);

		_playerCamera.transform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
		transform.Rotate(Vector3.up * heading);
	}

	//////////////////////////////////////////////////////////////////////
	private void CalculateAerialSettings()
	{
		// Formula : deltaMovement = velocityInitial * time + (acceleration * time^2) / 2  -->  where acceleration = gravity and velocityInitial is null
		_gravity = -(2f * _jumpHeight) / Mathf.Pow(_timeToJumpApex, 2f);
		// Formula : velocityFinal = velocityInitial + acceleration * time  -->  where velocityFinal = jumpVelocity and velocityInitial is null
		_jumpVelocity = Mathf.Abs(_gravity) * _timeToJumpApex;
	}
}
