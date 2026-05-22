using System.Collections;
using UnityEngine;

public class GolfPlayerController : MonoBehaviour
{
    public enum RotateAxis
    {
        LocalX,
        LocalY,
        LocalZ
    }
    [Header("Visibility")]
    public GameObject playerVisualRoot;
    public bool hidePlayerWhenFired = true;
    public bool snapPlayerToBallWhenAimStarts = true;

    [Header("References")]
    public CustomPhysicsBall ball;
    public Transform playerRoot;
    public Transform clubPivot;

    [Header("Position Around Ball")]
    public float sideOffset = -1.0f;
    public float backOffset = -0.5f;
    public float groundOffset = 0f;

    [Header("Smoothing")]
    public float moveSmooth = 8f;
    public float rotateSmooth = 10f;
    public float clubSmooth = 10f;

    [Header("Club Vertical Aim")]
    public RotateAxis verticalAimAxis = RotateAxis.LocalX;
    public bool invertVerticalAim = true;
    public float verticalAimMultiplier = 1f;

    [Header("Power Backswing")]
    public RotateAxis backswingAxis = RotateAxis.LocalX;
    public float minBackswingAngle = 0f;
    public float maxBackswingAngle = -80f;
    public bool invertPowerBackswing = false;

    [Header("Swing")]
    public float swingForwardAngle = -70f;
    public float swingDuration = 0.18f;
    public float returnDuration = 0.25f;

    private CustomPhysicsBall.ShotPhase previousPhase;
    private bool isSwinging = false;

    void Start()
    {
        if (ball != null)
            previousPhase = ball.currentPhase;
    }

    void Update()
    {
        if (ball == null || playerRoot == null) return;

        DetectSwingMoment();

        bool phaseChanged = previousPhase != ball.currentPhase;

        if (phaseChanged)
        {
            OnPhaseChanged(previousPhase, ball.currentPhase);
        }

        if (ball.currentPhase == CustomPhysicsBall.ShotPhase.Fired)
        {
            if (hidePlayerWhenFired)
                SetPlayerVisible(false);

            previousPhase = ball.currentPhase;
            return;
        }

        SetPlayerVisible(true);

        UpdatePlayerPositionAndRotation();
        UpdateClubPoseByPhase();

        previousPhase = ball.currentPhase;
    }      

    private void UpdatePlayerPositionAndRotation()
    {
        Quaternion aimRotation = Quaternion.Euler(0f, ball.horizontalAngle, 0f);

        Vector3 forward = aimRotation * Vector3.forward;
        Vector3 right = aimRotation * Vector3.right;

        Vector3 targetPosition =
            ball.transform.position
            + right * sideOffset
            + forward * backOffset
            + Vector3.up * groundOffset;

        playerRoot.position = Vector3.Lerp(
            playerRoot.position,
            targetPosition,
            Time.deltaTime * moveSmooth
        );

        playerRoot.rotation = Quaternion.Slerp(
            playerRoot.rotation,
            aimRotation,
            Time.deltaTime * rotateSmooth
        );
    }

    private void UpdateClubPoseByPhase()
    {
        if (clubPivot == null || isSwinging) return;

        switch (ball.currentPhase)
        {
            case CustomPhysicsBall.ShotPhase.Phase1_HorizontalAngle:
                SetClubLocalRotationSmooth(Quaternion.identity);
                break;

            case CustomPhysicsBall.ShotPhase.Phase2_VerticalAngle:
                ApplyVerticalAimPose();
                break;

            case CustomPhysicsBall.ShotPhase.Phase3_PowerCharge:
                ApplyBackswingPose();
                break;

            case CustomPhysicsBall.ShotPhase.Fired:
                break;
        }
    }

    private void ApplyVerticalAimPose()
    {
        float angle = ball.verticalAngle * verticalAimMultiplier;
        if (invertVerticalAim) angle = -angle;

        Quaternion targetRotation = MakeLocalRotation(verticalAimAxis, angle);
        SetClubLocalRotationSmooth(targetRotation);
    }

    private void ApplyBackswingPose()
    {
        float t = 0f;

        if (ball.maxPower > 0f)
            t = Mathf.Clamp01(ball.power / ball.maxPower);

        float targetMaxAngle = invertPowerBackswing
            ? -maxBackswingAngle
            : maxBackswingAngle;

        float angle = Mathf.Lerp(minBackswingAngle, targetMaxAngle, t);

        Quaternion targetRotation = MakeLocalRotation(backswingAxis, angle);
        SetClubLocalRotationSmooth(targetRotation);
    }

    private void DetectSwingMoment()
    {
        bool justFired =
            previousPhase == CustomPhysicsBall.ShotPhase.Phase3_PowerCharge &&
            ball.currentPhase == CustomPhysicsBall.ShotPhase.Fired;

        if (justFired && !isSwinging)
        {
            StartCoroutine(SwingRoutine());
        }
    }

    private IEnumerator SwingRoutine()
    {
        isSwinging = true;

        Quaternion startRotation = clubPivot.localRotation;
        Quaternion swingRotation = MakeLocalRotation(backswingAxis, swingForwardAngle);

        float timer = 0f;

        while (timer < swingDuration)
        {
            timer += Time.deltaTime;
            float t = timer / swingDuration;
            clubPivot.localRotation = Quaternion.Slerp(startRotation, swingRotation, t);
            yield return null;
        }

        timer = 0f;

        while (timer < returnDuration)
        {
            timer += Time.deltaTime;
            float t = timer / returnDuration;
            clubPivot.localRotation = Quaternion.Slerp(swingRotation, Quaternion.identity, t);
            yield return null;
        }

        isSwinging = false;
    }

    private void SetPlayerVisible(bool visible)
    {
        if (playerVisualRoot != null)
        {
            playerVisualRoot.SetActive(visible);
        }
    }
    private Quaternion MakeLocalRotation(RotateAxis axis, float angle)
    {
        switch (axis)
        {
            case RotateAxis.LocalX:
                return Quaternion.Euler(angle, 0f, 0f);

            case RotateAxis.LocalY:
                return Quaternion.Euler(0f, angle, 0f);

            case RotateAxis.LocalZ:
                return Quaternion.Euler(0f, 0f, angle);

            default:
                return Quaternion.identity;
        }
    }

    private void SetClubLocalRotationSmooth(Quaternion targetRotation)
    {
        clubPivot.localRotation = Quaternion.Slerp(
            clubPivot.localRotation,
            targetRotation,
            Time.deltaTime * clubSmooth
        );
    }
    private void OnPhaseChanged(
    CustomPhysicsBall.ShotPhase from,
    CustomPhysicsBall.ShotPhase to
    )
    {
        // 공이 멈춰서 다시 조준 페이즈로 돌아온 순간
        if (from == CustomPhysicsBall.ShotPhase.Fired &&
            to == CustomPhysicsBall.ShotPhase.Phase1_HorizontalAngle)
        {
            SetPlayerVisible(true);

            if (snapPlayerToBallWhenAimStarts)
            {
                SnapPlayerToCurrentBallPosition();
            }
        }

        // 발사되는 순간
        if (to == CustomPhysicsBall.ShotPhase.Fired)
        {
            if (hidePlayerWhenFired)
                SetPlayerVisible(false);
        }
    }
    private void SnapPlayerToCurrentBallPosition()
    {
        if (ball == null || playerRoot == null) return;

        Quaternion aimRotation = Quaternion.Euler(0f, ball.horizontalAngle, 0f);

        Vector3 forward = aimRotation * Vector3.forward;
        Vector3 right = aimRotation * Vector3.right;

        Vector3 targetPosition =
            ball.transform.position
            + right * sideOffset
            + forward * backOffset
            + Vector3.up * groundOffset;

        playerRoot.position = targetPosition;
        playerRoot.rotation = aimRotation;
    }
}