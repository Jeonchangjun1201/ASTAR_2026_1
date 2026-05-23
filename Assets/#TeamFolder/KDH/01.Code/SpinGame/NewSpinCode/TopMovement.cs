using UnityEngine;

public class TopMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 720f; // 팽이 자전 속도

    [Header("References")]
    [SerializeField] private FixedJoystick joystick; // ← 기존 패키지 그대로 사용

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (joystick == null)
            joystick = FindObjectOfType<FixedJoystick>();
    }

    void FixedUpdate()
    {
        Move();
        SpinTop();
    }

    void Move()
    {
        // FixedJoystick의 Horizontal, Vertical 값 사용
        Vector3 moveDir = new Vector3(joystick.Horizontal, 0f, joystick.Vertical);

        rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);

        // 이동 방향으로 팽이 회전
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRot, 10f * Time.fixedDeltaTime);
        }
    }

    void SpinTop()
    {
        // 팽이 Y축 자전 (항상 돌아감)
        transform.Rotate(Vector3.up, rotationSpeed * Time.fixedDeltaTime, Space.Self);
    }
}