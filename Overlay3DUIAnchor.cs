using UnityEngine;

public class Overlay3DUIAnchor : MonoBehaviour
{
    public enum AnchorPreset
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center
    }

    [Header("Camera")]
    public Camera overlayCamera;

    [Header("Anchor")]
    public AnchorPreset anchorPreset = AnchorPreset.TopRight;

    [Tooltip("화면 가장자리에서 떨어질 픽셀 거리")]
    public Vector2 pixelOffset = new Vector2(-120f, -120f);

    [Tooltip("카메라 앞쪽 거리. Orthographic에서는 Near/Far 사이에만 있으면 됨")]
    public float distanceFromCamera = 5f;

    [Header("Root Rotation")]
    [Tooltip("UI Root가 카메라를 정면으로 바라보게 할지")]
    public bool matchCameraRotation = true;

    [Tooltip("카메라 회전을 따라간 뒤 추가로 더할 Root 회전값")]
    public Vector3 rootRotationOffset = Vector3.zero;

    [Header("Pivot Rotation")]
    [Tooltip("실제로 회전시키고 싶은 피벗 오브젝트. 예: WindCompassPivot")]
    public Transform rotationPivot;

    [Tooltip("피벗에 적용할 로컬 회전값")]
    public Vector3 pivotLocalEuler = Vector3.zero;

    [Tooltip("체크하면 매 프레임 pivotLocalEuler 값을 Pivot에 적용")]
    public bool applyPivotRotation = true;

    [Header("Scale")]
    public bool keepScale = true;
    public Vector3 fixedLocalScale = Vector3.one;

    void LateUpdate()
    {
        if (overlayCamera == null)
        {
            overlayCamera = Camera.main;
            if (overlayCamera == null) return;
        }

        UpdateAnchoredPosition();
        UpdateRootRotation();
        UpdatePivotRotation();
        UpdateScale();
    }

    private void UpdateAnchoredPosition()
    {
        Vector2 anchor = GetAnchor01(anchorPreset);

        float screenX = anchor.x * Screen.width + pixelOffset.x;
        float screenY = anchor.y * Screen.height + pixelOffset.y;

        Vector3 screenPosition = new Vector3(
            screenX,
            screenY,
            distanceFromCamera
        );

        transform.position = overlayCamera.ScreenToWorldPoint(screenPosition);
    }

    private void UpdateRootRotation()
    {
        if (!matchCameraRotation) return;

        transform.rotation =
            overlayCamera.transform.rotation *
            Quaternion.Euler(rootRotationOffset);
    }

    private void UpdatePivotRotation()
    {
        if (!applyPivotRotation) return;
        if (rotationPivot == null) return;

        rotationPivot.localRotation = Quaternion.Euler(pivotLocalEuler);
    }

    private void UpdateScale()
    {
        if (!keepScale) return;

        transform.localScale = fixedLocalScale;
    }

    private Vector2 GetAnchor01(AnchorPreset preset)
    {
        switch (preset)
        {
            case AnchorPreset.TopLeft:
                return new Vector2(0f, 1f);

            case AnchorPreset.TopRight:
                return new Vector2(1f, 1f);

            case AnchorPreset.BottomLeft:
                return new Vector2(0f, 0f);

            case AnchorPreset.BottomRight:
                return new Vector2(1f, 0f);

            case AnchorPreset.Center:
                return new Vector2(0.5f, 0.5f);

            default:
                return new Vector2(1f, 1f);
        }
    }
}