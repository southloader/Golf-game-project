using UnityEngine;

public class GolfCameraDirector : MonoBehaviour
{
    [Header("References")]
    public CustomPhysicsBall ball;
    public Transform ballTransform;
    public Rigidbody ballRigidbody;

    [Header("Aim Target")]
    public Transform holeCupTarget;
    public bool alignAimToHoleWhenAimStarts = true;
    public bool alignAimToHoleOnStart = true;

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

    [Header("Cinematic Override")]
    public float cinematicMoveSmooth = 3f;
    public float cinematicRotateSmooth = 5f;
    public bool cinematicLookAtBallIfNoTarget = true;
    public float cinematicBallLookHeight = 1.0f;

    [Header("Cinematic Rules")]
    public bool exitCinematicWhenShotFired = true;

    private bool isCinematicActive = false;
    private Transform cinematicCameraPoint;
    private Transform cinematicLookTarget;

    private CustomPhysicsBall.ShotPhase previousPhase;
    private Vector3 lastFlatMoveDirection = Vector3.forward;

    void Start()
    {
        if (ball != null)
            previousPhase = ball.currentPhase;

        if (ballTransform != null)
        {
            if (alignAimToHoleOnStart)
                AlignAimAngleToHole();

            lastFlatMoveDirection = GetAimForward();

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

        // 핵심: 시네마틱 존 안에 있으면 기존 카메라워크를 덮어쓴다.
        if (isCinematicActive && cinematicCameraPoint != null)
        {
            UpdateCinematicCamera();
            return;
        }

        Vector3 targetPosition = GetTargetPosition();
        Vector3 lookTarget = GetLookTarget();

        float moveSmooth = GetCurrentMoveSmooth();
        float rotateSmooth = GetCurrentRotateSmooth();

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * moveSmooth
        );

        LookAt(lookTarget, false, rotateSmooth);
    }

    private void UpdateCinematicCamera()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            cinematicCameraPoint.position,
            Time.deltaTime * cinematicMoveSmooth
        );

        Vector3 lookTarget;

        if (cinematicLookTarget != null)
        {
            lookTarget = cinematicLookTarget.position;
        }
        else if (cinematicLookAtBallIfNoTarget && ballTransform != null)
        {
            lookTarget = ballTransform.position + Vector3.up * cinematicBallLookHeight;
        }
        else
        {
            // LookTarget이 없고 공도 안 보게 할 경우,
            // CinematicCameraPoint의 회전을 그대로 따라간다.
            Quaternion targetRot = cinematicCameraPoint.rotation;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * cinematicRotateSmooth
            );

            return;
        }

        LookAt(lookTarget, false, cinematicRotateSmooth);
    }

    public void EnterCinematicZone(Transform cameraPoint, Transform lookTarget = null)
    {
        // 시네마틱 카메라는 공이 날아가는 중에만 작동
        if (ball == null || ball.currentPhase != CustomPhysicsBall.ShotPhase.Fired)
            return;

        isCinematicActive = true;
        cinematicCameraPoint = cameraPoint;
        cinematicLookTarget = lookTarget;
    }

    public void ExitCinematicZone(Transform cameraPoint)
    {
        // 여러 시네마틱 존이 겹쳤을 때,
        // 현재 사용 중인 카메라 포인트를 가진 존만 해제하게 함.
        if (cinematicCameraPoint != cameraPoint)
            return;

        isCinematicActive = false;
        cinematicCameraPoint = null;
        cinematicLookTarget = null;

        // 복귀 순간에 추적 방향을 다시 잡아준다.
        if (ball.currentPhase == CustomPhysicsBall.ShotPhase.Fired)
        {
            Vector3 velocity = GetBallVelocity();
            Vector3 flatVelocity = new Vector3(velocity.x, 0f, velocity.z);

            if (flatVelocity.magnitude > minFollowSpeed)
                lastFlatMoveDirection = flatVelocity.normalized;
        }
        else
        {
            lastFlatMoveDirection = GetAimForward();
        }
    }
    private void AlignAimAngleToHole()
    {
        if (ball == null || ballTransform == null || holeCupTarget == null)
            return;

        Vector3 directionToHole = holeCupTarget.position - ballTransform.position;

        // 수평 방향만 사용
        directionToHole.y = 0f;

        if (directionToHole.sqrMagnitude < 0.001f)
            return;

        directionToHole.Normalize();

        // Unity 기준:
        // +Z 방향이 0도
        // +X 방향이 90도
        float angleToHole = Mathf.Atan2(directionToHole.x, directionToHole.z) * Mathf.Rad2Deg;

        ball.horizontalAngle = angleToHole;
    }

    private void OnPhaseChanged(CustomPhysicsBall.ShotPhase from, CustomPhysicsBall.ShotPhase to)
    {
        // 공이 멈춰서 다시 조준 상태로 돌아온 순간
        if (from == CustomPhysicsBall.ShotPhase.Fired &&
            to == CustomPhysicsBall.ShotPhase.Phase1_HorizontalAngle)
        {
            if (alignAimToHoleWhenAimStarts)
                AlignAimAngleToHole();

            lastFlatMoveDirection = GetAimForward();

            // 공이 시네마틱 존 안에서 멈춰도 조준 상태에서는 기본 카메라로 복귀
            ForceExitCinematicZone();
        }

        // 공을 새로 치는 순간
        if (to == CustomPhysicsBall.ShotPhase.Fired)
        {
            lastFlatMoveDirection = GetAimForward();

            // 공을 치는 순간에도 시네마틱 카메라 강제 해제
            if (exitCinematicWhenShotFired)
            {
                ForceExitCinematicZone();
            }
        }
    }
    public void ForceExitCinematicZone()
    {
        isCinematicActive = false;
        cinematicCameraPoint = null;
        cinematicLookTarget = null;

        if (ball != null && ball.currentPhase == CustomPhysicsBall.ShotPhase.Fired)
        {
            Vector3 velocity = GetBallVelocity();
            Vector3 flatVelocity = new Vector3(velocity.x, 0f, velocity.z);

            if (flatVelocity.magnitude > minFollowSpeed)
            {
                lastFlatMoveDirection = flatVelocity.normalized;
            }
            else
            {
                lastFlatMoveDirection = GetAimForward();
            }
        }
        else
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
            lastFlatMoveDirection = flatVelocity.normalized;

        return ballTransform.position
               - lastFlatMoveDirection * followDistance
               + Vector3.up * followHeight;
    }

    private Vector3 GetFollowLookTarget()
    {
        Vector3 velocity = GetBallVelocity();
        Vector3 flatVelocity = new Vector3(velocity.x, 0f, velocity.z);

        if (flatVelocity.magnitude > minFollowSpeed)
            lastFlatMoveDirection = flatVelocity.normalized;

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
        // Unity 버전에 따라 에러 나면 아래 사용
        // return ballRigidbody.velocity;
    }

    private float GetCurrentMoveSmooth()
    {
        if (ball.currentPhase == CustomPhysicsBall.ShotPhase.Fired)
            return followMoveSmooth;

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