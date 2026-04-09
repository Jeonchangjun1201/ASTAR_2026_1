using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpinController : MonoBehaviour
{
    [Header("스핀")]
    [SerializeField] private float rotationSpeed = 80f;
    [SerializeField] private float rotationDeceleration = 5f;
    
    [Header("멈추는 스핀")]
    [SerializeField] private float wobbleIntensity = 2f;
    [SerializeField] private float driftForce = 3f;

    private Rigidbody rb;
    private float initialSpeed;

    private void Awake() 
    {
        rb = GetComponent<Rigidbody>();
        initialSpeed = rotationSpeed;

        rb.constraints = RigidbodyConstraints.None; 
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.05f;
    }

    private void FixedUpdate()
    {
        if (rotationSpeed > 0.1f)
        {
            ApplySpinAndWobble();
            HandleDeceleration();
        }
    }

    private void ApplySpinAndWobble()
    {
        rb.angularVelocity = new Vector3(rb.angularVelocity.x, rotationSpeed, rb.angularVelocity.z);

        float speedRatio = rotationSpeed / initialSpeed;
        float wobbleFactor = (1f - speedRatio) * wobbleIntensity;

        if (wobbleFactor > 0.1f)
        {
            float wobbleX = Mathf.Sin(Time.time * 10f) * wobbleFactor;
            float wobbleZ = Mathf.Cos(Time.time * 10f) * wobbleFactor;
            
            rb.AddTorque(new Vector3(wobbleX, 0, wobbleZ), ForceMode.Acceleration);

            Vector3 driftDir = new Vector3(wobbleX, 0, wobbleZ).normalized;
            rb.AddForce(driftDir * driftForce * (1f - speedRatio), ForceMode.Force);
        }
    }

    private void HandleDeceleration()
    {
        rotationSpeed -= rotationDeceleration * Time.fixedDeltaTime;
        if (rotationSpeed < 0) rotationSpeed = 0;
    }
}