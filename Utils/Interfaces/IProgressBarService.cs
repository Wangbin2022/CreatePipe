namespace CreatePipe.Utils.Interfaces
{
    public interface IProgressBarService
    {
        void Start(int maximum, string initialTitle = "准备中...");
        void Update(int currentValue, string currentItemName);
        void Stop();
    }
}
