using UnityEngine;
using TMPro;

public class HoleCup : MonoBehaviour
{
    [Header("References")]
    public TMP_Text resultText;

    [Header("Settings")]
    public string ballTag = "GolfBall";
    public float maxHoleInSpeed = 3.0f;
    public bool stopBallOnHoleIn = true;

    [Header("Debug")]
    public bool ignoreSpeedForTest = true;

    private bool isHoled = false;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[HoleCup] OnTriggerEnter: {other.name}");
        TryHoleIn(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHoleIn(other);
    }

    private void TryHoleIn(Collider other)
    {
        if (isHoled) return;

        // 태그 확인
        if (!other.CompareTag(ballTag))
        {
            // 공의 Collider가 자식에 있고 태그가 부모에 있을 수도 있어서 부모도 확인
            if (other.transform.root == null || !other.transform.root.CompareTag(ballTag))
                return;
        }

        CustomPhysicsBall ball = other.GetComponent<CustomPhysicsBall>();
        if (ball == null)
            ball = other.GetComponentInParent<CustomPhysicsBall>();

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null)
            rb = other.GetComponentInParent<Rigidbody>();

        if (ball == null)
        {
            Debug.LogWarning("[HoleCup] CustomPhysicsBall을 찾지 못했어. 공 오브젝트에 스크립트가 붙어있는지 확인해.");
            return;
        }

        if (rb == null)
        {
            Debug.LogWarning("[HoleCup] Rigidbody를 찾지 못했어. 공 오브젝트에 Rigidbody가 있는지 확인해.");
            return;
        }

        float speed = rb.linearVelocity.magnitude;
        // Unity 버전에 따라 linearVelocity 에러 나면 rb.velocity.magnitude로 바꿔.

        if (!ignoreSpeedForTest && speed > maxHoleInSpeed)
        {
            Debug.Log($"[HoleCup] 속도가 너무 빨라서 홀인 처리 안 함. Speed: {speed}");
            return;
        }

        isHoled = true;

        if (stopBallOnHoleIn)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;

            other.transform.position = transform.position + Vector3.up * 0.08f;
        }

        if (resultText != null)
        {
            resultText.text = $"HOLE IN!\n타수: {ball.strokeCount}";
        }

        Debug.Log($"[HoleCup] HOLE IN! Strokes: {ball.strokeCount}");
    }
}