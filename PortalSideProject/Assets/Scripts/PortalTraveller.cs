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
    [SerializeField] private GameObject _meshObject = null;
    [SerializeField] private Collider _travellerCollider = null;

    private GameObject _portalClone;
    private Material[] _travellerMaterials;
    private Material[] _cloneMaterials;

    // Portal
    private float _portalSideSign;
    private bool _isInPortal;
    private bool _isPortalTracked;

    //////////////////////////////////////////////////////////////////////
    public GameObject PortalClone { get { return _portalClone; } }
    public Collider TravellerCollider { get { return _travellerCollider; } }
    public int PhysLayer { get { return gameObject.layer; } }
    public bool IsInPortal { get { return _isInPortal; } }

    //////////////////////////////////////////////////////////////////////
    virtual protected void Awake()
	{
	}

    //////////////////////////////////////////////////////////////////////
    virtual protected void Start()
    {
        _portalClone = Instantiate(_meshObject, _meshObject.transform.parent);
        _portalClone.transform.localScale = _meshObject.transform.localScale;
        _portalClone.SetActive(false);

		_travellerMaterials = GetSliceableMaterials(_meshObject);
		_cloneMaterials = GetSliceableMaterials(_portalClone);
    }

    //////////////////////////////////////////////////////////////////////
    public void OnApproachPortalZone(Portal portal, int physZoneTraveller, int physZoneClone)
    {
        if (!_isPortalTracked)
        {
            _isPortalTracked = true;
            portal.OnTravellerApprochingPortal(this);

            SetNewPhysLayer(physZoneTraveller, physZoneClone);

            _portalSideSign = portal.GetSideOfPortal(transform.position);
        }
    }

	//////////////////////////////////////////////////////////////////////
	public void OnLeavePortalZone(Portal portal)
	{
		if (_isPortalTracked && !_isInPortal)
		{
			_isPortalTracked = false;
			portal.OnTravellerLeavingPortal(this);

            int layerPortalNone = GlobalVars.Instance._layerPortalNone;
            SetNewPhysLayer(layerPortalNone, layerPortalNone);
        }
	}

	//////////////////////////////////////////////////////////////////////
	public void OnEnterPortal()
	{
        if (_isPortalTracked && !_isInPortal)
        {
            _isInPortal = true;
            _portalClone.SetActive(true);

            int travellerLayer = (gameObject.layer == GlobalVars.Instance._layerPortalSideA) ? GlobalVars.Instance._layerPortalSideA_Exclusive : GlobalVars.Instance._layerPortalSideB_Exclusive;
            int cloneLayer = (gameObject.layer == GlobalVars.Instance._layerPortalSideA) ? GlobalVars.Instance._layerPortalSideB_Exclusive : GlobalVars.Instance._layerPortalSideA_Exclusive;
            SetNewPhysLayer(travellerLayer, cloneLayer);
        }
    }

    //////////////////////////////////////////////////////////////////////
    public void OnExitPortal()
    {
        if (_isPortalTracked && _isInPortal)
        {
            _isInPortal = false;
            _portalClone.SetActive(false);

			int travellerLayer = (gameObject.layer == GlobalVars.Instance._layerPortalSideA_Exclusive) ? GlobalVars.Instance._layerPortalSideA : GlobalVars.Instance._layerPortalSideB;
			int cloneLayer = (gameObject.layer == GlobalVars.Instance._layerPortalSideA_Exclusive) ? GlobalVars.Instance._layerPortalSideB : GlobalVars.Instance._layerPortalSideA;
			SetNewPhysLayer(travellerLayer, cloneLayer);

			DisableMaterialsSlice();
		}
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
        Vector3 oldPos = transform.position;
        Quaternion oldRot = transform.rotation;

        transform.SetPositionAndRotation(newPos, newRot);
        UpdateClone(oldPos, oldRot);

		int travellerLayer = (gameObject.layer == GlobalVars.Instance._layerPortalSideA_Exclusive) ? GlobalVars.Instance._layerPortalSideB_Exclusive : GlobalVars.Instance._layerPortalSideA_Exclusive;
		int cloneLayer = (gameObject.layer == GlobalVars.Instance._layerPortalSideA_Exclusive) ? GlobalVars.Instance._layerPortalSideA_Exclusive : GlobalVars.Instance._layerPortalSideB_Exclusive;
		SetNewPhysLayer(travellerLayer, cloneLayer);

		Physics.SyncTransforms();
    }

    //////////////////////////////////////////////////////////////////////
    public void UpdateClone(Vector3 newPos, Quaternion newRot)
    {
        _portalClone.transform.SetPositionAndRotation(newPos, newRot);
    }

	//////////////////////////////////////////////////////////////////////
	public bool IsClone(Collider collider)
	{
        Collider cloneCollider =_portalClone.GetComponent<Collider>();
        return (cloneCollider == collider);
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
    public void DisableMaterialsSlice()
    {
		foreach (Material travellerMaterial in _travellerMaterials)
		{
			travellerMaterial.SetVector("_SliceNormal", Vector3.zero);
		}

		foreach (Material cloneMaterial in _cloneMaterials)
		{
			cloneMaterial.SetVector("_SliceNormal", Vector3.zero);
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
	public void SetNewPhysLayer(int travellerLayer, int cloneLayer)
	{
		_meshObject.layer = gameObject.layer = travellerLayer;
		_portalClone.layer = cloneLayer;
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
}
