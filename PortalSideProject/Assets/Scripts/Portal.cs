using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    private const float NEAR_CLIP_OFFSET = 0.05f;
    private const float NEAR_CLIP_LIMIT = 0.05f;

    [Header("Portal")]
    [SerializeField] private Portal _linkedPortal = null;
    [SerializeField] private MeshRenderer _portalScreen = null;

    // Render
    private RenderTexture _viewTexture;
    private Camera _portalCamera;
    private Camera _playerCamera;

    // Travellers
    private List<PortalTraveller> _teleportingTravellers;

    //////////////////////////////////////////////////////////////////////
    void Awake()
    {
        _portalCamera = GetComponentInChildren<Camera>();
        _playerCamera = Camera.main;
        _teleportingTravellers = new List<PortalTraveller>();
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
    public void OnTravellerEnterPortal(PortalTraveller traveller)
    {
        if (!_teleportingTravellers.Contains(traveller))
        {
            _teleportingTravellers.Add(traveller);
        }
    }

    //////////////////////////////////////////////////////////////////////
    public void OnTravellerExitPortal(PortalTraveller traveller)
    {
        _teleportingTravellers.Remove(traveller);
    }

    //////////////////////////////////////////////////////////////////////
    private void UpdateTravellersSlice()
    {
        foreach (PortalTraveller traveller in _teleportingTravellers)
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
                travellerSliceParams._sliceOffsetDist = -_portalScreen.transform.localScale.z;
            }

            cloneSliceParams._sliceOffsetDist = 0;
            if (_linkedPortal.GetSideOfPortal(_playerCamera.transform.position) == GetSideOfPortal(traveller.transform.position))
            {
                cloneSliceParams._sliceOffsetDist = -_portalScreen.transform.localScale.z;
            }

            traveller.UpdateMaterialsSlice(travellerSliceParams, cloneSliceParams);
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
        for (int i = _teleportingTravellers.Count - 1; i >= 0; --i)
        {
            PortalTraveller portalTraveller = _teleportingTravellers[i];

            Matrix4x4 relativeMat = _linkedPortal.transform.localToWorldMatrix * transform.worldToLocalMatrix * portalTraveller.transform.localToWorldMatrix;
            if (portalTraveller.HasCrossedPortal(this))
            {
                portalTraveller.Teleport(relativeMat.GetColumn(3), relativeMat.rotation);

                _linkedPortal.OnTravellerEnterPortal(portalTraveller);
                _teleportingTravellers.RemoveAt(i);
            }
            else
            {
                portalTraveller.UpdateClone(relativeMat.GetColumn(3), relativeMat.rotation);
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

        foreach (PortalTraveller traveller in _teleportingTravellers)
        {
            float travellerSide = GetSideOfPortal(traveller.transform.position);
            traveller.OverrideSliceOffsetDist((travellerSide == portalCamSide) ? -OFFSET_DIST : OFFSET_DIST, false);

            float cloneSide = -travellerSide;
            traveller.OverrideSliceOffsetDist((cloneSide == linkedPortalCamSide) ? screenThickness : -screenThickness, true);
        }

		foreach (PortalTraveller linkedTraveller in _linkedPortal._teleportingTravellers)
		{
			float travellerSide = _linkedPortal.GetSideOfPortal(linkedTraveller.transform.position);
            linkedTraveller.OverrideSliceOffsetDist((travellerSide != portalCamSide) ? -OFFSET_DIST : OFFSET_DIST, true);

            linkedTraveller.OverrideSliceOffsetDist(((travellerSide == linkedPortalCamSide)) ? screenThickness : -screenThickness, false);
		}
	}
}
