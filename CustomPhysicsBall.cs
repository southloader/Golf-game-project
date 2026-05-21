using UnityEngine;

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

    private float currentBounciness;
    private float currentFriction;

    private Rigidbody rb;
    private int powerDirection = 1;
    private bool isCharging = false;
    private Vector3 lastVelocity; 

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; 

        currentBounciness = defaultBounciness;
        currentFriction = defaultFriction;
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

    private void OnCollisionEnter(Collision collision)
    {
        if (currentPhase != ShotPhase.Fired) return;

        Vector3 normalVec = collision.contacts[0].normal;
        Vector3 reflectVec = Vector3.Reflect(lastVelocity, normalVec);
        rb.linearVelocity = reflectVec * currentBounciness;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (currentPhase != ShotPhase.Fired) return;
        rb.linearVelocity -= rb.linearVelocity * currentFriction * Time.fixedDeltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("dirt"))
        {
            currentBounciness = dirtBounciness;
            currentFriction = dirtFriction;
        }
        else if (other.CompareTag("sand"))
        {
            currentBounciness = sandBounciness;
            currentFriction = sandFriction;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("dirt") || other.CompareTag("sand"))
        {
            currentBounciness = defaultBounciness;
            currentFriction = defaultFriction;
        }
    }
}