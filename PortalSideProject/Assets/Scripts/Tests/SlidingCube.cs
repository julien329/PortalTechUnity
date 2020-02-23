using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SlidingCube : PortalTraveller
{
	[SerializeField] private float _maxSpeed = 5f;
	[SerializeField] private float _velocitySmoothTime = 0.5f;

	private Rigidbody _rigidBody;
	private Vector3 _currentVelocity;
	private Vector3 _smoothVelocity;

	//////////////////////////////////////////////////////////////////////
	protected override void Awake()
	{
		base.Awake();
		_rigidBody = GetComponent<Rigidbody>();
	}

	//////////////////////////////////////////////////////////////////////
	void Update()
	{
		float inputX = Input.GetAxis("HorizontalAlt");
		float inputZ = Input.GetAxis("VerticalAlt");

		Vector3 worldInputs = Vector3.ClampMagnitude(transform.right * inputX + transform.forward * inputZ, 1f);
		Vector3 targetVelocity = worldInputs * _maxSpeed;

		_currentVelocity = Vector3.SmoothDamp(_currentVelocity, targetVelocity, ref _smoothVelocity, _velocitySmoothTime);
		_rigidBody.MovePosition(transform.position + targetVelocity * Time.deltaTime);
	}
}
