using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalTraveller : MonoBehaviour
{
    public struct SliceParameters
    {
        public Vector3 _slicePosition;
        public Vector3 _sliceNormal;
        public float _sliceOffsetDist;
    }

    [Header("PortalTraveller")]
    [SerializeField] private GameObject _meshObjectToClone = null;

    private GameObject _portalClone;
    private Material[] _travellerMaterials;
    private Material[] _cloneMaterials;

    // Portal
    private float _portalSideSign;
    private bool _isInPortal;

	// Layers
	int _layerPortalNone;
    int _layerPortalSideA;
    int _layerPortalSideA_Exclusive;
    int _layerPortalSideB;
    int _layerPortalSideB_Exclusive;

    //////////////////////////////////////////////////////////////////////
    public GameObject PortalClone { get { return _portalClone; } }

    //////////////////////////////////////////////////////////////////////
    virtual protected void Awake()
	{
        _layerPortalNone = LayerMask.NameToLayer("PortalNone");
        _layerPortalSideA = LayerMask.NameToLayer("PortalSideA");
        _layerPortalSideA_Exclusive = LayerMask.NameToLayer("PortalSideA_Exclusive");
        _layerPortalSideB = LayerMask.NameToLayer("PortalSideB");
        _layerPortalSideB_Exclusive = LayerMask.NameToLayer("PortalSideB_Exclusive");
	}

    //////////////////////////////////////////////////////////////////////
    virtual protected void Start()
    {
        _portalClone = Instantiate(_meshObjectToClone, _meshObjectToClone.transform.parent);
        _portalClone.transform.localScale = _meshObjectToClone.transform.localScale;
        _portalClone.SetActive(false);

		_travellerMaterials = GetSliceableMaterials(_meshObjectToClone);
		_cloneMaterials = GetSliceableMaterials(_portalClone);

        gameObject.layer = _meshObjectToClone.layer = _portalClone.layer = _layerPortalNone;
    }

	//////////////////////////////////////////////////////////////////////
	void OnTriggerEnter(Collider other)
	{
        if (other.tag == "Portal")
        {
            if (!_isInPortal)
            {
                Portal portal = other.GetComponent<Portal>();
                portal.OnTravellerEnterPortal(this);

                OnEnterPortal(portal);
            }
        }
        else if (other.tag == "ZoneA_Detection")
        {
			if (_meshObjectToClone.layer == _layerPortalNone)
			{
				SetNewPhysLayer(_layerPortalSideA);
			}
        }
		else if (other.tag == "ZoneB_Detection")
		{
			if (_meshObjectToClone.layer == _layerPortalNone)
			{
				SetNewPhysLayer(_layerPortalSideB);
			}
		}
	}

	//////////////////////////////////////////////////////////////////////
	void OnTriggerExit(Collider other)
	{
		if (other.tag == "Portal")
		{
            if (_isInPortal)
            {
                Portal portal = other.GetComponent<Portal>();
                portal.OnTravellerExitPortal(this);

                OnExitPortal();
			}
		}
		else if (other.tag == "ZoneA_Detection")
		{
            if (_meshObjectToClone.layer == _layerPortalSideA)
            {
                SetNewPhysLayer(_layerPortalNone);
            }
        }
		else if (other.tag == "ZoneB_Detection")
		{
			if (_meshObjectToClone.layer == _layerPortalSideB)
			{
				SetNewPhysLayer(_layerPortalNone);
			}
		}
	}

	//////////////////////////////////////////////////////////////////////
	public void OnEnterPortal(Portal portal)
	{
        _isInPortal = true;

        _portalSideSign = portal.GetSideOfPortal(transform.position);
        _portalClone.SetActive(true);

        SetNewPhysLayer((_portalSideSign > 0) ? _layerPortalSideA_Exclusive : _layerPortalSideB_Exclusive);
    }

    //////////////////////////////////////////////////////////////////////
    public void OnExitPortal()
    {
        _isInPortal = false;

        _portalClone.SetActive(false);

        SetNewPhysLayer((_meshObjectToClone.layer == _layerPortalSideA_Exclusive) ? _layerPortalSideA : _layerPortalSideB);
    }

    //////////////////////////////////////////////////////////////////////
    public bool HasCrossedPortal(Portal portal)
    {
        float oldPortalSideSign = _portalSideSign;
        float newPortalSideSign = portal.GetSideOfPortal(transform.position);

        _portalSideSign = newPortalSideSign;

        return newPortalSideSign != oldPortalSideSign;
    }

    //////////////////////////////////////////////////////////////////////
    public void Teleport(Vector3 newPos, Quaternion newRot)
    {
        UpdateClone(transform.position, transform.rotation);
        transform.SetPositionAndRotation(newPos, newRot);

        SetNewPhysLayer((_portalSideSign > 0) ? _layerPortalSideA_Exclusive : _layerPortalSideB_Exclusive);

		Physics.SyncTransforms();
    }

    //////////////////////////////////////////////////////////////////////
    public void UpdateClone(Vector3 newPos, Quaternion newRot)
    {
        _portalClone.transform.SetPositionAndRotation(newPos, newRot);
    }

    //////////////////////////////////////////////////////////////////////
    public void UpdateMaterialsSlice(SliceParameters travellerSliceParams, SliceParameters cloneSliceParams)
    {
        foreach (Material travellerMaterial in _travellerMaterials)
        {
            travellerMaterial.SetVector("_SliceCenter", travellerSliceParams._slicePosition);
            travellerMaterial.SetVector("_SliceNormal", travellerSliceParams._sliceNormal);
            travellerMaterial.SetFloat("_SliceOffsetDist", travellerSliceParams._sliceOffsetDist);
        }

		foreach (Material cloneMaterial in _cloneMaterials)
		{
            cloneMaterial.SetVector("_SliceCenter", cloneSliceParams._slicePosition);
            cloneMaterial.SetVector("_SliceNormal", cloneSliceParams._sliceNormal);
            cloneMaterial.SetFloat("_SliceOffsetDist", cloneSliceParams._sliceOffsetDist);
        }
	}

	//////////////////////////////////////////////////////////////////////
	public void OverrideSliceOffsetDist(float sliceOffsetDist, bool isClone)
	{
        if (!isClone)
        {
			foreach (Material travellerMaterial in _travellerMaterials)
			{
				travellerMaterial.SetFloat("_SliceOffsetDist", sliceOffsetDist);
			}
		}
        else
        {
			foreach (Material cloneMaterial in _cloneMaterials)
			{
                cloneMaterial.SetFloat("_SliceOffsetDist", sliceOffsetDist);
			}
		}
	}

	//////////////////////////////////////////////////////////////////////
	private Material[] GetSliceableMaterials(GameObject gameObject)
    {
        List<Material> sliceableMaterials = new List<Material>();

        MeshRenderer[] meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            foreach (Material material in meshRenderer.materials)
            {
                if (material.shader.name.Equals("Custom/MaterialSlicer"))
                {
                    sliceableMaterials.Add(material);
                }
            }
        }

        return sliceableMaterials.ToArray();
    }

    //////////////////////////////////////////////////////////////////////
    private void SetNewPhysLayer(int travellerPhysLayer)
    {
        int clonePhysLayer = _layerPortalNone;

        if (travellerPhysLayer == _layerPortalSideA_Exclusive)
        {
            clonePhysLayer = _layerPortalSideB_Exclusive;
        }
        else if (travellerPhysLayer == _layerPortalSideB_Exclusive)
        {
            clonePhysLayer = _layerPortalSideA_Exclusive;
        }

        _meshObjectToClone.layer = gameObject.layer = travellerPhysLayer;
        _portalClone.layer = clonePhysLayer;
    }
}
