namespace Core.UI
{
    public interface IPanel
    {
        bool IsVisible { get; }
        void Show();
        void Hide();
    }
}
