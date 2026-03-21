using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float runSpeed = 9f;
    public float crouchSpeed = 3f;

    [Header("Gravity")]
    public float jumpHeight = 6f;
    public float gravity = -20f;

    private CharacterController controller;

    private Vector3 moveDirection;
    private float speed;
    private float verticalVelocity;

    private bool isGround;
    private bool isRun;
    private bool isCrouching = false;

    private float h;
    private float v;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        GetInput();
        SetRun();
        SetSpeed();
        SetMove();
        SetJumpAndGravity();
    }

    void GetInput()
    {
        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");
    }

    void SetRun()
    {
        isRun = Input.GetKey(KeyCode.LeftShift) && !isCrouching && v > 0;
    }

    void SetSpeed()
    {
        if (isRun)
        {
            speed = runSpeed;
        }
        else if (isCrouching)
        {
            speed = crouchSpeed;
        }
        else
        {
            speed = walkSpeed;
        }
    }

    void SetMove()
    {
        Vector3 move = transform.forward * v + transform.right * h;

        if (move.magnitude > 1f)
            move.Normalize();

        moveDirection = move;
    }

    void SetJumpAndGravity()
    {
        isGround = controller.isGrounded;

        if (isGround)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = jumpHeight;
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 finalMove =
            moveDirection * speed +
            Vector3.up * verticalVelocity;

        controller.Move(finalMove * Time.deltaTime);
    }
}