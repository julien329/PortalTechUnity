using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface CollisionHandler
{
	void OnEnterTrigger(Collider localCollider, Collider externalCollider);
	void OnExitTrigger(Collider localCollider, Collider externalCollider);
}
