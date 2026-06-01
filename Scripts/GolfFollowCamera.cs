using UnityEngine;

public class GolfFollowCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform ball;
    public Rigidbody ballRigidbody;

    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(0f, 5f, -9f);
    public float followSmooth = 4f;
    public float rotateSmooth = 5f;

    [Header("Look Settings")]
    public float lookAheadDistance = 4f;
    public float lookHeight = 1.2f;
    public float minMoveSpeed = 0.3f;

    [Header("Anti Jitter")]
    public bool ignoreVerticalBounce = true;
    public float lookTargetSmooth = 6f;
    public float heightSmooth = 3f;

    [Header("State")]
    public bool isFollowing = false;

    private Vector3 smoothedLookTarget;
    private Vector3 lastFlatDirection = Vector3.forward;
    private float smoothedBallHeight;

    void Start()
    {
        if (ball != null)
        {
            smoothedLookTarget = ball.position;
            smoothedBallHeight = ball.position.y;
        }
    }

    void LateUpdate()
    {
        if (!isFollowing || ball == null)
            return;

        Vector3 ballVelocity = GetBallVelocity();

        // 핵심: 카메라 방향 계산에서는 y속도를 제거한다.
        Vector3 flatVelocity = new Vector3(ballVelocity.x, 0f, ballVelocity.z);
        float flatSpeed = flatVelocity.magnitude;

        if (flatSpeed > minMoveSpeed)
        {
            lastFlatDirection = flatVelocity.normalized;
        }

        // 공이 튀어도 카메라 높이가 즉각 출렁이지 않게 y값을 부드럽게 따라감
        if (ignoreVerticalBounce)
        {
            smoothedBallHeight = Mathf.Lerp(
                smoothedBallHeight,
                ball.position.y,
                Time.deltaTime * heightSmooth
            );
        }
        else
        {
            smoothedBallHeight = ball.position.y;
        }

        Vector3 stableBallPosition = new Vector3(
            ball.position.x,
            smoothedBallHeight,
            ball.position.z
        );

        // 공 진행 방향 뒤쪽에 카메라 배치
        Quaternion directionRotation = Quaternion.LookRotation(lastFlatDirection, Vector3.up);
        Vector3 targetPosition = stableBallPosition + directionRotation * offset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * followSmooth
        );

        // 공 자체보다 살짝 앞쪽을 바라보게 함
        Vector3 targetLookPoint =
            stableBallPosition
            + lastFlatDirection * lookAheadDistance
            + Vector3.up * lookHeight;

        smoothedLookTarget = Vector3.Lerp(
            smoothedLookTarget,
            targetLookPoint,
            Time.deltaTime * lookTargetSmooth
        );

        Vector3 lookDirection = smoothedLookTarget - transform.position;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotateSmooth
            );
        }
    }

    private Vector3 GetBallVelocity()
    {
        if (ballRigidbody == null)
            return Vector3.zero;

        // Unity 6이면 linearVelocity, 이전 버전이면 velocity 사용
        return ballRigidbody.linearVelocity;
        // return ballRigidbody.velocity;
    }

    public void StartFollow()
    {
        isFollowing = true;

        if (ball != null)
        {
            smoothedLookTarget = ball.position;
            smoothedBallHeight = ball.position.y;
        }
    }

    public void StopFollow()
    {
        isFollowing = false;
    }
}