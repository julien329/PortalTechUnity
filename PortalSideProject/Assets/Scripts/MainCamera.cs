using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour
{
    // Portals
    Portal[] _portals;

    //////////////////////////////////////////////////////////////////////
    void Awake()
    {
        _portals = FindObjectsOfType<Portal>();
    }

    //////////////////////////////////////////////////////////////////////
    void OnPreCull()
    {
		foreach (Portal portal in _portals)
		{
			portal.OnPreRenderView();
		}

		foreach (Portal portal in _portals)
        {
            portal.RenderView();
        }

		foreach (Portal portal in _portals)
		{
			portal.OnPostRenderView();
		}
	}
}
