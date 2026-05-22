using UnityEngine;

public class GolfAimVisual3D : MonoBehaviour
{
    public enum VerticalRotateAxis
    {
        LocalX,
        LocalY,
        LocalZ
    }

    [Header("References")]
    public CustomPhysicsBall ball;

    [Header("Aim Root")]
    public Transform aimVisualRoot;
    public float rootHeightOffset = 0.3f;

    [Header("Arrow Objects")]
    public GameObject horizontalArrow;
    public Transform verticalArrowPivot;
    public GameObject verticalArrow;
    public GameObject powerTankRoot;

    [Header("Power Fill - Y Axis Only")]
    public Transform powerFill;
    public float minPowerHeight = 0.01f;
    public float maxPowerHeight = 4.0f;

    [Header("Vertical Arrow Rotation")]
    public VerticalRotateAxis verticalRotateAxis = VerticalRotateAxis.LocalX;
    public bool invertVerticalAngle = true;

    [Header("Debug")]
    public bool showAllVisualsForDebug = false;

    private Vector3 originalPowerFillScale;

    void Start()
    {
        if (powerFill != null)
        {
            originalPowerFillScale = powerFill.localScale;
        }
    }

    void Update()
    {
        if (ball == null || aimVisualRoot == null) return;

        UpdateRootPositionAndHorizontalAngle();
        UpdateVerticalAngle();
        UpdatePowerBarYOnly();
        UpdateVisibility();
    }

    private void UpdateRootPositionAndHorizontalAngle()
    {
        aimVisualRoot.position = ball.transform.position + Vector3.up * rootHeightOffset;

        aimVisualRoot.rotation = Quaternion.Euler(
            0f,
            ball.horizontalAngle,
            0f
        );
    }

    private void UpdateVerticalAngle()
    {
        if (verticalArrowPivot == null) return;

        float angle = invertVerticalAngle ? -ball.verticalAngle : ball.verticalAngle;

        switch (verticalRotateAxis)
        {
            case VerticalRotateAxis.LocalX:
                verticalArrowPivot.localRotation = Quaternion.Euler(angle, 0f, 0f);
                break;

            case VerticalRotateAxis.LocalY:
                verticalArrowPivot.localRotation = Quaternion.Euler(0f, angle, 0f);
                break;

            case VerticalRotateAxis.LocalZ:
                verticalArrowPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
                break;
        }
    }

    private void UpdatePowerBarYOnly()
    {
        if (powerFill == null) return;

        float t = 0f;

        if (ball.maxPower > 0f)
        {
            t = Mathf.Clamp01(ball.power / ball.maxPower);
        }

        float height = Mathf.Lerp(minPowerHeight, maxPowerHeight, t);

        Vector3 scale = originalPowerFillScale;
        scale.y = height;

        powerFill.localScale = scale;

        powerFill.localPosition = new Vector3(
            0f,
            height * 0.5f,
            0f
        );
    }

    private void UpdateVisibility()
    {
        bool phase1 = ball.currentPhase == CustomPhysicsBall.ShotPhase.Phase1_HorizontalAngle;
        bool phase2 = ball.currentPhase == CustomPhysicsBall.ShotPhase.Phase2_VerticalAngle;
        bool phase3 = ball.currentPhase == CustomPhysicsBall.ShotPhase.Phase3_PowerCharge;
        bool fired = ball.currentPhase == CustomPhysicsBall.ShotPhase.Fired;

        // AimVisualRoot 자체는 계속 켜둔다.
        // 대신 내부 시각 요소만 페이즈별로 켜고 끈다.
        if (aimVisualRoot != null)
            aimVisualRoot.gameObject.SetActive(true);

        if (horizontalArrow != null)
            horizontalArrow.SetActive(phase1);

        if (verticalArrow != null)
             verticalArrow.SetActive(phase2);

    if (powerTankRoot != null)
        powerTankRoot.SetActive(phase3);

    // 발사 중이면 모든 조준 요소를 숨김
    if (fired)
    {
        if (horizontalArrow != null)
            horizontalArrow.SetActive(false);

        if (verticalArrow != null)
            verticalArrow.SetActive(false);

        if (powerTankRoot != null)
            powerTankRoot.SetActive(false);
    }
}
}