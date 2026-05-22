using UnityEngine;

public class GolfCameraDirector : MonoBehaviour
{
    [Header("References")]
    public CustomPhysicsBall ball;
    public Transform ballTransform;
    public Rigidbody ballRigidbody;

    [Header("Phase 1 - Horizontal Aim")]
    public float phase1Distance = 7f;
    public float phase1Height = 5f;
    public float phase1LookHeight = 0.6f;

    [Header("Phase 2 - Vertical Aim")]
    public float phase2SideDistance = 6f;
    public float phase2BackDistance = 1.5f;
    public float phase2Height = 3f;
    public float phase2LookHeight = 1.2f;

    [Header("Phase 3 - Power")]
    public float phase3Distance = 7f;
    public float phase3Height = 3.5f;
    public float phase3LookHeight = 1.0f;

    [Header("Fired - Follow")]
    public float followDistance = 9f;
    public float followHeight = 5f;
    public float followLookAhead = 3f;
    public float minFollowSpeed = 0.3f;

    [Header("Smoothing")]
    public float aimMoveSmooth = 5f;
    public float aimRotateSmooth = 7f;
    public float followMoveSmooth = 4f;
    public float followRotateSmooth = 5f;

    [Header("Return To Aim")]
    public float returnMoveSmooth = 6f;
    public float returnRotateSmooth = 8f;
    public bool snapToAimWhenPhaseChanges = false;

    private CustomPhysicsBall.ShotPhase previousPhase;
    private Vector3 lastFlatMoveDirection = Vector3.forward;

    void Start()
    {
        if (ball != null)
        {
            previousPhase = ball.currentPhase;
        }

        if (ballTransform != null)
        {
            Vector3 initialForward = GetAimForward();
            lastFlatMoveDirection = initialForward;

            transform.position = GetPhase1Position();
            LookAt(GetPhase1LookTarget(), true);
        }
    }

    void LateUpdate()
    {
        if (ball == null || ballTransform == null) return;

        bool phaseChanged = previousPhase != ball.currentPhase;

        if (phaseChanged)
        {
            OnPhaseChanged(previousPhase, ball.currentPhase);
            previousPhase = ball.currentPhase;
        }

        Vector3 targetPosition = GetTargetPosition();
        Vector3 lookTarget = GetLookTarget();

        float moveSmooth = GetCurrentMoveSmooth();
        float rotateSmooth = GetCurrentRotateSmooth();

        if (snapToAimWhenPhaseChanges && phaseChanged && ball.currentPhase != CustomPhysicsBall.ShotPhase.Fired)
        {
            transform.position = targetPosition;
            LookAt(lookTarget, true);
            return;
        }

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * moveSmooth
        );

        LookAt(lookTarget, false, rotateSmooth);
    }

    private void OnPhaseChanged(CustomPhysicsBall.ShotPhase from, CustomPhysicsBall.ShotPhase to)
    {
        // 공이 멈추고 Fired에서 Phase1로 돌아온 순간
        // 추적 중이던 방향을 버리고, 현재 조준 방향 기준으로 다시 정렬한다.
        if (from == CustomPhysicsBall.ShotPhase.Fired &&
            to == CustomPhysicsBall.ShotPhase.Phase1_HorizontalAngle)
        {
            lastFlatMoveDirection = GetAimForward();
        }

        // Phase3에서 발사되는 순간에는 현재 발사 방향을 추적 방향 초기값으로 삼는다.
        if (to == CustomPhysicsBall.ShotPhase.Fired)
        {
            lastFlatMoveDirection = GetAimForward();
        }
    }

    private Vector3 GetTargetPosition()
    {
        switch (ball.currentPhase)
        {
            case CustomPhysicsBall.ShotPhase.Phase1_HorizontalAngle:
                return GetPhase1Position();

            case CustomPhysicsBall.ShotPhase.Phase2_VerticalAngle:
                return GetPhase2Position();

            case CustomPhysicsBall.ShotPhase.Phase3_PowerCharge:
                return GetPhase3Position();

            case CustomPhysicsBall.ShotPhase.Fired:
                return GetFollowPosition();

            default:
                return GetPhase1Position();
        }
    }

    private Vector3 GetLookTarget()
    {
        switch (ball.currentPhase)
        {
            case CustomPhysicsBall.ShotPhase.Phase1_HorizontalAngle:
                return GetPhase1LookTarget();

            case CustomPhysicsBall.ShotPhase.Phase2_VerticalAngle:
                return GetPhase2LookTarget();

            case CustomPhysicsBall.ShotPhase.Phase3_PowerCharge:
                return GetPhase3LookTarget();

            case CustomPhysicsBall.ShotPhase.Fired:
                return GetFollowLookTarget();

            default:
                return GetPhase1LookTarget();
        }
    }

    private Vector3 GetPhase1Position()
    {
        Vector3 forward = GetAimForward();
        Vector3 ballPos = ballTransform.position;

        return ballPos - forward * phase1Distance + Vector3.up * phase1Height;
    }

    private Vector3 GetPhase1LookTarget()
    {
        return ballTransform.position + Vector3.up * phase1LookHeight;
    }

    private Vector3 GetPhase2Position()
    {
        Vector3 forward = GetAimForward();
        Vector3 right = GetAimRight();
        Vector3 ballPos = ballTransform.position;

        // 옆에서 보되, 너무 완전한 측면이면 방향감이 사라져서 살짝 뒤로 뺌
        return ballPos + right * phase2SideDistance - forward * phase2BackDistance + Vector3.up * phase2Height;
    }

    private Vector3 GetPhase2LookTarget()
    {
        return ballTransform.position + Vector3.up * phase2LookHeight;
    }

    private Vector3 GetPhase3Position()
    {
        Vector3 forward = GetAimForward();
        Vector3 ballPos = ballTransform.position;

        return ballPos - forward * phase3Distance + Vector3.up * phase3Height;
    }

    private Vector3 GetPhase3LookTarget()
    {
        Vector3 forward = GetAimForward();

        return ballTransform.position
               + Vector3.up * phase3LookHeight
               + forward * 1.5f;
    }

    private Vector3 GetFollowPosition()
    {
        Vector3 velocity = GetBallVelocity();
        Vector3 flatVelocity = new Vector3(velocity.x, 0f, velocity.z);

        if (flatVelocity.magnitude > minFollowSpeed)
        {
            lastFlatMoveDirection = flatVelocity.normalized;
        }

        return ballTransform.position
               - lastFlatMoveDirection * followDistance
               + Vector3.up * followHeight;
    }

    private Vector3 GetFollowLookTarget()
    {
        Vector3 velocity = GetBallVelocity();
        Vector3 flatVelocity = new Vector3(velocity.x, 0f, velocity.z);

        if (flatVelocity.magnitude > minFollowSpeed)
        {
            lastFlatMoveDirection = flatVelocity.normalized;
        }

        return ballTransform.position
               + Vector3.up * 1.0f
               + lastFlatMoveDirection * followLookAhead;
    }

    private Vector3 GetAimForward()
    {
        Quaternion horizontalRotation = Quaternion.Euler(0f, ball.horizontalAngle, 0f);
        return horizontalRotation * Vector3.forward;
    }

    private Vector3 GetAimRight()
    {
        Quaternion horizontalRotation = Quaternion.Euler(0f, ball.horizontalAngle, 0f);
        return horizontalRotation * Vector3.right;
    }

    private Vector3 GetBallVelocity()
    {
        if (ballRigidbody == null)
            return Vector3.zero;

        return ballRigidbody.linearVelocity;

        // Unity 버전에 따라 linearVelocity에서 에러가 나면 위 줄 대신 아래 줄 사용
        // return ballRigidbody.velocity;
    }

    private float GetCurrentMoveSmooth()
    {
        if (ball.currentPhase == CustomPhysicsBall.ShotPhase.Fired)
            return followMoveSmooth;

        // Fired에서 Phase1로 돌아온 직후도 결국 조준 상태이므로 aim smooth 사용
        return aimMoveSmooth;
    }

    private float GetCurrentRotateSmooth()
    {
        if (ball.currentPhase == CustomPhysicsBall.ShotPhase.Fired)
            return followRotateSmooth;

        return aimRotateSmooth;
    }

    private void LookAt(Vector3 target, bool instant, float smooth = 10f)
    {
        Vector3 direction = target - transform.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        if (instant)
        {
            transform.rotation = targetRotation;
        }
        else
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * smooth
            );
        }
    }
}