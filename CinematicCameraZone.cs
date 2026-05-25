using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CinematicCameraZone : MonoBehaviour
{
    [Header("References")]
    public GolfCameraDirector cameraDirector;

    [Tooltip("카메라가 이동할 위치")]
    public Transform cinematicCameraPoint;

    [Tooltip("카메라가 바라볼 지점. 비워두면 공을 바라봄")]
    public Transform lookTarget;

    [Header("Target")]
    public string ballTag = "Player";

    [Header("Debug")]
    public bool drawGizmos = true;
    public Color gizmoColor = new Color(1f, 0.8f, 0f, 0.25f);

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(ballTag))
            return;

        if (cameraDirector == null || cinematicCameraPoint == null)
            return;

        cameraDirector.EnterCinematicZone(cinematicCameraPoint, lookTarget);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(ballTag))
            return;

        if (cameraDirector == null || cinematicCameraPoint == null)
            return;

        cameraDirector.ExitCinematicZone(cinematicCameraPoint);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = gizmoColor;
        Collider col = GetComponent<Collider>();

        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }

        if (cinematicCameraPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(cinematicCameraPoint.position, 0.4f);
            Gizmos.DrawLine(transform.position, cinematicCameraPoint.position);
        }

        if (lookTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(lookTarget.position, 0.3f);
            if (cinematicCameraPoint != null)
                Gizmos.DrawLine(cinematicCameraPoint.position, lookTarget.position);
        }
    }
}