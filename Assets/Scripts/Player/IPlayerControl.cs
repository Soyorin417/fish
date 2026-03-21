public interface IPlayerControl
{
    void SetMovementEnabled(bool enabled);
    void SetCameraEnabled(bool enabled);
    void SetInputEnabled(bool enabled);
    bool IsGrounded();
}
