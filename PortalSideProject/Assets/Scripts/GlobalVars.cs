using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalVars
{
	private static GlobalVars _instance;

	public int _layerPortalNone;
	public int _layerPortalSideA;
	public int _layerPortalSideA_Exclusive;
	public int _layerPortalSideB;
    public int _layerPortalSideB_Exclusive;

	//////////////////////////////////////////////////////////////////////
	public static GlobalVars Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new GlobalVars();
			}

			return _instance;
		}
	}

	//////////////////////////////////////////////////////////////////////
	GlobalVars()
	{
		_layerPortalNone = LayerMask.NameToLayer("PortalNone");
		_layerPortalSideA = LayerMask.NameToLayer("PortalSideA");
		_layerPortalSideA_Exclusive = LayerMask.NameToLayer("PortalSideA_Exclusive");
		_layerPortalSideB = LayerMask.NameToLayer("PortalSideB");
		_layerPortalSideB_Exclusive = LayerMask.NameToLayer("PortalSideB_Exclusive");
	}
}
