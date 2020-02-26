using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CollisionListener : MonoBehaviour
{
	private CollisionHandler _collisionHandler = null;
	private Collider _collider = null;

	//////////////////////////////////////////////////////////////////////
	void Awake()
	{
		_collisionHandler = GetComponentInParent<CollisionHandler>();
		_collider = GetComponent<Collider>();
	}

	//////////////////////////////////////////////////////////////////////
	void OnTriggerEnter(Collider collider)
	{
		_collisionHandler.OnEnterTrigger(_collider, collider);
	}

	//////////////////////////////////////////////////////////////////////
	void OnTriggerExit(Collider collider)
	{
		_collisionHandler.OnExitTrigger(_collider, collider);
	}
}
