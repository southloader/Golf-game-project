using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class CustomPhysicsBall : MonoBehaviour
{
    public enum ShotPhase
    {
        Phase1_HorizontalAngle,
        Phase2_VerticalAngle,
        Phase3_PowerCharge,
        Fired
    }

    [Header("Current State")]
    public ShotPhase currentPhase = ShotPhase.Phase1_HorizontalAngle;

    [Header("Launch Settings")]
    public float power = 0f;
    public float horizontalAngle = 0f;
    public float verticalAngle = 45f;

    [Header("Limits & Speeds")]
    public float maxPower = 30f;
    public float powerSpeed = 20f;
    public float angleSpeed = 45f;

    [Header("Custom Physics (환경 변수)")]
    public Vector3 windForce = Vector3.zero;
    
    public float defaultBounciness = 0.6f;
    public float defaultFriction = 0.5f;
    public float dirtBounciness = 0.3f;
    public float dirtFriction = 2.0f;
    public float sandBounciness = 0.1f;
    public float sandFriction = 4.0f;

    [Header("Trajectory Prediction")]
    public LineRenderer lineRenderer;
    public int maxSimulationSteps = 200;    
    public float simulationTimeStep = 0.02f; 
    public float dashSpacing = 0.5f;        
    public float dashAnimationSpeed = 2.0f; // [추가됨] 점선이 흘러가는 속도

    private float currentBounciness;
    private float currentFriction;
    private Rigidbody rb;
    private int powerDirection = 1;
    private bool isCharging = false;
    private Vector3 lastVelocity; 

    public float test = 1f; 

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; 

        currentBounciness = defaultBounciness;
        currentFriction = defaultFriction;

        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();

        // [점선 텍스처 생성]
        Texture2D dashTexture = new Texture2D(32, 1, TextureFormat.RGBA32, false);
        dashTexture.wrapMode = TextureWrapMode.Repeat; 
        
        for (int i = 0; i < 32; i++)
        {
            dashTexture.SetPixel(i, 0, i < 16 ? Color.white : Color.clear);
        }
        dashTexture.Apply();

        Material dashedMat = new Material(Shader.Find("Sprites/Default"));
        dashedMat.mainTexture = dashTexture;
        
        lineRenderer.material = dashedMat;
        lineRenderer.textureMode = LineTextureMode.Tile;
        lineRenderer.material.mainTextureScale = new Vector2(dashSpacing, 1f);
    }

    void Update()
    {
        switch (currentPhase)
        {
            case ShotPhase.Phase1_HorizontalAngle: HandlePhase1(); break;
            case ShotPhase.Phase2_VerticalAngle: HandlePhase2(); break;
            case ShotPhase.Phase3_PowerCharge: HandlePhase3(); break;
            case ShotPhase.Fired: break;
        }

        if (currentPhase != ShotPhase.Fired)
        {
            PredictTrajectory();
            AnimateDashedLine(); // [추가됨] 궤적을 예측함과 동시에 점선을 애니메이션 시킴
        }
        else
        {
            lineRenderer.positionCount = 0;
        }
    }

    void FixedUpdate()
    {
        if (currentPhase == ShotPhase.Fired)
        {
            rb.linearVelocity += windForce * Time.fixedDeltaTime;
            lastVelocity = rb.linearVelocity;
        }
    }

    private void HandlePhase1()
    {
        if (Input.GetKey(KeyCode.A)) horizontalAngle -= angleSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) horizontalAngle += angleSpeed * Time.deltaTime;
        if (Input.GetKeyUp(KeyCode.Space)) currentPhase = ShotPhase.Phase2_VerticalAngle;
    }

    private void HandlePhase2()
    {
        if (Input.GetKey(KeyCode.W)) verticalAngle += angleSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) verticalAngle -= angleSpeed * Time.deltaTime;
        verticalAngle = Mathf.Clamp(verticalAngle, 0f, 90f);
        if (Input.GetKeyUp(KeyCode.Space))
        {
            currentPhase = ShotPhase.Phase3_PowerCharge;
            power = 0f; powerDirection = 1; isCharging = false;
        }
    }

    private void HandlePhase3()
    {
        if (Input.GetKeyDown(KeyCode.Space)) isCharging = true;

        if (isCharging && Input.GetKey(KeyCode.Space))
        {
            power += powerSpeed * powerDirection * Time.deltaTime;
            if (power >= maxPower) { power = maxPower; powerDirection = -1; }
            else if (power <= 0f) { power = 0f; powerDirection = 1; }
        }

        if (isCharging && Input.GetKeyUp(KeyCode.Space)) Shoot();
    }

    private void Shoot()
    {
        Vector3 shootDirection = Quaternion.Euler(-verticalAngle, horizontalAngle, 0f) * Vector3.forward;
        rb.useGravity = true;
        rb.linearVelocity = shootDirection * power;
        currentPhase = ShotPhase.Fired;
    }

    // =====================================================================
    // [추가됨] 점선 텍스처를 움직이게 만드는 함수
    // =====================================================================
    private void AnimateDashedLine()
    {
        if (lineRenderer.material != null)
        {
            // Time.time을 이용해 시간이 지날수록 offset 값을 계속 변화시킵니다.
            // - 부호를 붙여야 공이 날아갈 방향(앞)으로 점선이 흐르게 됩니다.
            float offset = Time.time * dashAnimationSpeed;
            lineRenderer.material.mainTextureOffset = new Vector2(-offset, 0f);
        }
    }

    private void PredictTrajectory()
    {
        List<Vector3> trajectoryPoints = new List<Vector3>();

        Vector3 simPosition = transform.position;
        Vector3 shootDirection = Quaternion.Euler(-verticalAngle, horizontalAngle, 0f) * Vector3.forward;
        Vector3 simVelocity = shootDirection * power;
        
        bool isSimGrounded = false;
        float currentSimFriction = defaultFriction;

        trajectoryPoints.Add(simPosition);

        for (int i = 0; i < maxSimulationSteps; i++)
        {
            if (!isSimGrounded)
            {
                Vector3 nextPosition = simPosition + simVelocity * simulationTimeStep;
                Vector3 displacement = nextPosition - simPosition;

                Ray ray = new Ray(simPosition, displacement.normalized);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, displacement.magnitude))
                {
                    simPosition = hit.point;

                    float simBounciness = defaultBounciness;
                    currentSimFriction = defaultFriction;
                    if (hit.collider.CompareTag("dirt")) { simBounciness = dirtBounciness; currentSimFriction = dirtFriction; }
                    else if (hit.collider.CompareTag("sand")) { simBounciness = sandBounciness; currentSimFriction = sandFriction; }

                    if (simVelocity.magnitude < 0.5f)
                    {
                        isSimGrounded = true; 
                        simVelocity = new Vector3(simVelocity.x, 0f, simVelocity.z);
                        simPosition += hit.normal * 0.01f; 
                    }
                    else
                    {
                        simVelocity = Vector3.Reflect(simVelocity, hit.normal) * simBounciness;
                    }

                    trajectoryPoints.Add(simPosition);
                    continue; 
                }

                simVelocity += (Physics.gravity + windForce) * simulationTimeStep;
                simPosition = nextPosition;
            }
            else
            {
                float deceleration = currentSimFriction * 10f * simulationTimeStep;
                simVelocity = Vector3.MoveTowards(simVelocity, Vector3.zero, deceleration);
                simPosition += simVelocity * simulationTimeStep;

                if (simVelocity.magnitude < 0.1f)
                {
                    trajectoryPoints.Add(simPosition);
                    break; 
                }
            }

            trajectoryPoints.Add(simPosition);
            if (simPosition.y < -10f) break;
        }

        lineRenderer.positionCount = trajectoryPoints.Count;
        lineRenderer.SetPositions(trajectoryPoints.ToArray());
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentPhase != ShotPhase.Fired) return;

        if (lastVelocity.magnitude < 0.5f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            return;
        }

        Vector3 normalVec = collision.contacts[0].normal;
        Vector3 reflectVec = Vector3.Reflect(lastVelocity, normalVec);
        rb.linearVelocity = reflectVec * currentBounciness;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (currentPhase != ShotPhase.Fired) return;

        float deceleration = currentFriction * 10f * Time.fixedDeltaTime;
        rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, deceleration);

        if (rb.linearVelocity.magnitude < 0.1f)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero; 
            rb.useGravity = false; 

            currentPhase = ShotPhase.Phase1_HorizontalAngle;
            power = 0f;
            horizontalAngle = 0f;
            verticalAngle = 45f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("dirt")) { currentBounciness = dirtBounciness; currentFriction = dirtFriction; }
        else if (other.CompareTag("sand")) { currentBounciness = sandBounciness; currentFriction = sandFriction; }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("dirt") || other.CompareTag("sand")) { currentBounciness = defaultBounciness; currentFriction = defaultFriction; }
    }
}