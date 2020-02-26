using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour, CollisionHandler
{
    private const float ANTI_CLIP_OFFSET = 0.05f;
    private const float NEAR_CLIP_OFFSET = 0.05f;
    private const float NEAR_CLIP_LIMIT = 0.05f;

    [Header("Portal")]
    [SerializeField] private Portal _linkedPortal = null;
    [SerializeField] private MeshRenderer _portalScreen = null;
    [SerializeField] private Collider _physZoneA = null;
    [SerializeField] private Collider _physZoneB = null;
    [SerializeField] private Collider _portalZone = null;

    // Render
    private RenderTexture _viewTexture;
    private Camera _portalCamera;
    private Camera _playerCamera;

    // Travellers
    private List<PortalTraveller> _trackedTravellers;

	//////////////////////////////////////////////////////////////////////
	void Awake()
    {
        _portalCamera = GetComponentInChildren<Camera>();
        _playerCamera = Camera.main;
        _trackedTravellers = new List<PortalTraveller>();
	}

    //////////////////////////////////////////////////////////////////////
    void Start()
    {
        _portalCamera.enabled = false;
    }

    //////////////////////////////////////////////////////////////////////
    void LateUpdate()
    {
        UpdateTravellers();
    }

    //////////////////////////////////////////////////////////////////////
    public void OnEnterTrigger(Collider localCollider, Collider externalCollider)
    {
        PortalTraveller traveller = externalCollider.GetComponentInParent<PortalTraveller>();
        if (!traveller || traveller.TravellerCollider != externalCollider)
        {
            return;
        }

        if (localCollider == _physZoneA)
        {
            traveller.OnApproachPortalZone(this, GlobalVars.Instance._layerPortalSideA, GlobalVars.Instance._layerPortalSideB);
        }
        else if (localCollider == _physZoneB)
        {
            traveller.OnApproachPortalZone(this, GlobalVars.Instance._layerPortalSideB, GlobalVars.Instance._layerPortalSideA);
        }
		else if (localCollider == _portalZone)
		{
            traveller.OnEnterPortal();
        }
	}

	//////////////////////////////////////////////////////////////////////
	public void OnExitTrigger(Collider localCollider, Collider externalCollider)
	{
		PortalTraveller traveller = externalCollider.GetComponentInParent<PortalTraveller>();
		if (!traveller || traveller.TravellerCollider != externalCollider)
		{
			return;
		}

		if (localCollider == _physZoneA)
		{
            traveller.OnLeavePortalZone(this);
        }
		else if (localCollider == _physZoneB)
		{
            traveller.OnLeavePortalZone(this);
        }
		else if (localCollider == _portalZone)
		{
            if (_trackedTravellers.Contains(traveller))
            {
                traveller.OnExitPortal();
            }
        }
	}

	//////////////////////////////////////////////////////////////////////
	public void OnPreRenderView()
    {
        UpdateTravellersSlice();
    }

    //////////////////////////////////////////////////////////////////////
    public void RenderView()
    {
        if (!IsVisibleFromCamera(_playerCamera))
        {
            return;
        }

        _portalScreen.enabled = false;

        CreateViewTexture();
        MoveCameraToPosition();

        UpdateNearClipPlane();
        AvoidClipping();
        _portalCamera.Render();

        _portalScreen.enabled = true;
    }

    //////////////////////////////////////////////////////////////////////
    public void OnPostRenderView()
    {
        UpdateTravellersSlice();
        UpdateScreenThickness(_playerCamera.transform.position);
    }

    //////////////////////////////////////////////////////////////////////
    public float GetSideOfPortal(Vector3 position)
    {
        return Mathf.Sign(Vector3.Dot(position - transform.position, transform.forward));
    }

    //////////////////////////////////////////////////////////////////////
    public void OnTravellerApprochingPortal(PortalTraveller traveller)
    {
        if (!_trackedTravellers.Contains(traveller))
        {
            _trackedTravellers.Add(traveller);
        }
    }

    //////////////////////////////////////////////////////////////////////
    public void OnTravellerLeavingPortal(PortalTraveller traveller)
    {
        _trackedTravellers.Remove(traveller);
    }

    //////////////////////////////////////////////////////////////////////
    private void UpdateTravellersSlice()
    {
        foreach (PortalTraveller traveller in _trackedTravellers)
        {
            if (traveller.IsInPortal)
            {
                PortalTraveller.SliceParameters travellerSliceParams = new PortalTraveller.SliceParameters();
                PortalTraveller.SliceParameters cloneSliceParams = new PortalTraveller.SliceParameters();

                travellerSliceParams._slicePosition = transform.position;
                cloneSliceParams._slicePosition = _linkedPortal.transform.position;

                float travellerSideOfPortal = GetSideOfPortal(traveller.transform.position);
                travellerSliceParams._sliceNormal = transform.forward * -travellerSideOfPortal;
                cloneSliceParams._sliceNormal = _linkedPortal.transform.forward * travellerSideOfPortal;

                travellerSliceParams._sliceOffsetDist = 0;
                if (GetSideOfPortal(_playerCamera.transform.position) != GetSideOfPortal(traveller.transform.position))
                {
                    travellerSliceParams._sliceOffsetDist = -(_portalScreen.transform.localScale.z + ANTI_CLIP_OFFSET);
                }

                cloneSliceParams._sliceOffsetDist = 0;
                if (_linkedPortal.GetSideOfPortal(_playerCamera.transform.position) == GetSideOfPortal(traveller.transform.position))
				{
					cloneSliceParams._sliceOffsetDist = -(_portalScreen.transform.localScale.z + ANTI_CLIP_OFFSET);
                }

                traveller.UpdateMaterialsSlice(travellerSliceParams, cloneSliceParams);
            }
        }
    }

    //////////////////////////////////////////////////////////////////////
    private void MoveCameraToPosition()
    {
        Matrix4x4 relativeMat = transform.localToWorldMatrix * _linkedPortal.transform.worldToLocalMatrix * _playerCamera.transform.localToWorldMatrix;
        _portalCamera.transform.SetPositionAndRotation(relativeMat.GetColumn(3), relativeMat.rotation);
    }

    //////////////////////////////////////////////////////////////////////
    private void CreateViewTexture()
    {
        if (_viewTexture == null || _viewTexture.width != Screen.width || _viewTexture.height != Screen.height)
        {
            if (_viewTexture != null)
            {
                _viewTexture.Release();
            }

            _viewTexture = new RenderTexture(Screen.width, Screen.height, 0);
            _portalCamera.targetTexture = _viewTexture;

            _linkedPortal._portalScreen.material.SetTexture("_MainTex", _viewTexture);
        }
    }

    //////////////////////////////////////////////////////////////////////
    private bool IsVisibleFromCamera(Camera camera)
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(frustumPlanes, _linkedPortal._portalScreen.bounds);
    }

    //////////////////////////////////////////////////////////////////////
    private void UpdateTravellers()
    {
        for (int i = _trackedTravellers.Count - 1; i >= 0; --i)
        {
            PortalTraveller traveller = _trackedTravellers[i];
            if (traveller.IsInPortal)
            {
                Matrix4x4 relativeMat = _linkedPortal.transform.localToWorldMatrix * transform.worldToLocalMatrix * traveller.transform.localToWorldMatrix;
                if (traveller.HasCrossedPortal(this))
				{
					traveller.Teleport(relativeMat.GetColumn(3), relativeMat.rotation);

                    _linkedPortal.OnTravellerApprochingPortal(traveller);
                    OnTravellerLeavingPortal(traveller);
                }
                else
                {
                    traveller.UpdateClone(relativeMat.GetColumn(3), relativeMat.rotation);
                }
            }
        }
    }

    //////////////////////////////////////////////////////////////////////
    private void UpdateScreenThickness(Vector3 cameraPos)
    {
        // This prevents the screen from clipping with the camera's near clip plane
        float halfHeight = _playerCamera.nearClipPlane * Mathf.Tan(_playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * _playerCamera.aspect;

        float distToNearClipPlaneCorner = new Vector3(halfWidth, halfHeight, _playerCamera.nearClipPlane).magnitude;

        Transform screenTransform = _portalScreen.transform;
        screenTransform.localScale = new Vector3(screenTransform.localScale.x, screenTransform.transform.localScale.y, distToNearClipPlaneCorner);

        float translateSign = (Vector3.Dot(transform.forward, transform.position - cameraPos) > 0) ? 1f : -1f;
        screenTransform.localPosition = new Vector3(screenTransform.localPosition.x, screenTransform.transform.localPosition.y, distToNearClipPlaneCorner * translateSign * 0.5f);
    }

    //////////////////////////////////////////////////////////////////////
    private void UpdateNearClipPlane()
    {
        float vecToDotNormal = Mathf.Sign(Vector3.Dot(transform.forward, transform.position - _portalCamera.transform.position));

        Vector3 camSpacePosition = _portalCamera.worldToCameraMatrix.MultiplyPoint(transform.position);
        Vector3 camSpaceNormal = _portalCamera.worldToCameraMatrix.MultiplyVector(transform.forward) * vecToDotNormal;

        float camSpaceDistance = -Vector3.Dot(camSpacePosition, camSpaceNormal) + NEAR_CLIP_OFFSET;
        if (Mathf.Abs(camSpaceDistance) > NEAR_CLIP_LIMIT)
        {
            Vector4 clipPlaneCamSpace = new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDistance);
            _portalCamera.projectionMatrix = _playerCamera.CalculateObliqueMatrix(clipPlaneCamSpace);
        }
        else
        {
            _portalCamera.projectionMatrix = _playerCamera.projectionMatrix;
        }
    }

    //////////////////////////////////////////////////////////////////////
    private void AvoidClipping()
    {
        _linkedPortal.UpdateScreenThickness(_portalCamera.transform.position);
        float screenThickness = _linkedPortal._portalScreen.transform.localScale.z;

        float OFFSET_DIST = 1000f;

        float portalCamSide = GetSideOfPortal(_portalCamera.transform.position);
        float linkedPortalCamSide = _linkedPortal.GetSideOfPortal(_portalCamera.transform.position);

        foreach (PortalTraveller traveller in _trackedTravellers)
        {
            if (traveller.IsInPortal)
            {
                float travellerSide = GetSideOfPortal(traveller.transform.position);
                traveller.OverrideSliceOffsetDist((travellerSide == portalCamSide) ? -OFFSET_DIST : OFFSET_DIST, false);

                float cloneSide = -travellerSide;
                traveller.OverrideSliceOffsetDist((cloneSide == linkedPortalCamSide) ? (screenThickness + ANTI_CLIP_OFFSET) : -(screenThickness + ANTI_CLIP_OFFSET), true);
            }
        }

		foreach (PortalTraveller linkedTraveller in _linkedPortal._trackedTravellers)
		{
            if (linkedTraveller.IsInPortal)
            {
                float travellerSide = _linkedPortal.GetSideOfPortal(linkedTraveller.transform.position);
                linkedTraveller.OverrideSliceOffsetDist((travellerSide != portalCamSide) ? -OFFSET_DIST : OFFSET_DIST, true);

                linkedTraveller.OverrideSliceOffsetDist(((travellerSide == linkedPortalCamSide)) ? (screenThickness + ANTI_CLIP_OFFSET) : -(screenThickness + ANTI_CLIP_OFFSET), false);
            }
		}
	}
}
