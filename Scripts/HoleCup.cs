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
    public bool ignoreSpeedForTest = false;

    [Header("End Behavior")]
    public bool hideBallOnHoleIn = true;
    public float hideDelay = 0.2f;

    private bool isHoled = false;

    private void OnTriggerEnter(Collider other)
    {
        TryHoleIn(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHoleIn(other);
    }

    private void TryHoleIn(Collider other)
    {
        if (isHoled) return;

        if (!other.CompareTag(ballTag))
        {
            if (other.transform.root == null || !other.transform.root.CompareTag(ballTag))
                return;
        }

        CustomPhysicsBall ball = other.GetComponent<CustomPhysicsBall>();
        if (ball == null)
            ball = other.GetComponentInParent<CustomPhysicsBall>();

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null)
            rb = other.GetComponentInParent<Rigidbody>();

        if (ball == null || rb == null)
            return;

        float speed = rb.linearVelocity.magnitude;
        // Unity 버전에 따라 에러 나면 rb.velocity.magnitude로 교체

        if (!ignoreSpeedForTest && speed > maxHoleInSpeed)
            return;

        isHoled = true;

        if (stopBallOnHoleIn)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;

            ball.transform.position = transform.position + Vector3.up * 0.05f;
        }

        if (resultText != null)
        {
            resultText.text = $"HOLE IN!\n타수: {ball.strokeCount}";
        }

        if (hideBallOnHoleIn)
        {
            StartCoroutine(HideBallAfterDelay(ball.gameObject, hideDelay));
        }

        Debug.Log($"[HoleCup] HOLE IN! Strokes: {ball.strokeCount}");
    }

    private System.Collections.IEnumerator HideBallAfterDelay(GameObject ballObject, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (ballObject == null)
            yield break;

        TrailRenderer trail = ballObject.GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.emitting = false;
            trail.Clear();
        }

        Rigidbody rb = ballObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        Collider[] colliders = ballObject.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        Renderer[] renderers = ballObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        ballObject.SetActive(false);
    }
}
