public interface IInventoryView
{
    bool IsVisible { get; }
    void Show();
    void Hide();
}