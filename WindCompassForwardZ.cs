using UnityEngine;

public class WindCompassForwardZ : MonoBehaviour
{
    [Header("References")]
    public CustomPhysicsBall ball;

    [Tooltip("카메라 앞에 붙은 나침반 UI의 루트. 보통 WindCompassRoot")]
    public Transform compassRoot;

    [Tooltip("실제로 회전할 피벗. WindNeedlePivot")]
    public Transform needlePivot;

    [Tooltip("화살표 모델. 기본 상태에서 Local +Z 방향을 향해야 함")]
    public Transform needleModel;

    [Header("Rotation")]
    public bool invertDirection = false;

    [Tooltip("모델 방향이 90도, 180도 틀어져 있을 때 보정")]
    public float yawOffset = 0f;

    public float rotateSmooth = 12f;

    [Header("Wind Strength Display")]
    public bool scaleByWindStrength = true;

    [Tooltip("true면 화살표 길이를 Local Z 방향으로 늘림")]
    public bool stretchForwardByWind = true;

    public float minScale = 0.8f;
    public float maxScale = 1.4f;
    public float maxWindStrength = 10f;

    [Header("Visibility")]
    public bool hideWhenNoWind = false;
    public float noWindThreshold = 0.01f;

    private Vector3 initialNeedleScale;

    void Start()
    {
        if (compassRoot == null)
            compassRoot = transform;

        if (needleModel != null)
            initialNeedleScale = needleModel.localScale;
    }

    void Update()
    {
        if (ball == null || compassRoot == null || needlePivot == null)
            return;

        Vector3 worldWind = ball.windForce;

        // 바람 UI는 수평 방향만 표시한다.
        Vector3 flatWorldWind = new Vector3(worldWind.x, 0f, worldWind.z);
        float windStrength = flatWorldWind.magnitude;

        if (hideWhenNoWind)
            needlePivot.gameObject.SetActive(windStrength > noWindThreshold);

        if (windStrength <= noWindThreshold)
            return;

        UpdateNeedleRotation(flatWorldWind.normalized);
        UpdateNeedleScale(windStrength);
    }

    private void UpdateNeedleRotation(Vector3 flatWorldWindDir)
    {
        /*
         * 핵심:
         * WindCompassRoot가 카메라 자식이면,
         * 월드 바람 방향을 그대로 쓰면 카메라 회전에 따라 이상해진다.
         *
         * 그래서 월드 바람 방향을 compassRoot 기준의 로컬 방향으로 변환한다.
         */
        Vector3 localWindDir = compassRoot.InverseTransformDirection(flatWorldWindDir);

        // 나침반 바늘은 XZ 평면에서만 돈다.
        localWindDir.y = 0f;

        if (localWindDir.sqrMagnitude < 0.0001f)
            return;

        localWindDir.Normalize();

        /*
         * 모델의 기본 정면이 Local +Z이므로,
         * atan2(x, z)를 사용해서
         * +Z를 0도로 보는 Yaw 각도를 구한다.
         */
        float targetYaw = Mathf.Atan2(localWindDir.x, localWindDir.z) * Mathf.Rad2Deg;

        if (invertDirection)
            targetYaw = -targetYaw;

        targetYaw += yawOffset;

        Quaternion targetRotation = Quaternion.Euler(0f, targetYaw, 0f);

        needlePivot.localRotation = Quaternion.Slerp(
            needlePivot.localRotation,
            targetRotation,
            Time.deltaTime * rotateSmooth
        );
    }

    private void UpdateNeedleScale(float windStrength)
    {
        if (!scaleByWindStrength || needleModel == null)
            return;

        float t = Mathf.Clamp01(windStrength / maxWindStrength);
        float scaleValue = Mathf.Lerp(minScale, maxScale, t);

        Vector3 scale = initialNeedleScale;

        if (stretchForwardByWind)
        {
            // 화살표의 정면이 Local +Z이므로 길이는 Z축으로 늘린다.
            scale.z = initialNeedleScale.z * scaleValue;
        }
        else
        {
            // 전체 크기를 균일하게 키운다.
            scale = initialNeedleScale * scaleValue;
        }

        needleModel.localScale = scale;
    }
}