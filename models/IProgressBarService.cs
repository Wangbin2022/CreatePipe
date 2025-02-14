namespace CreatePipe.models
{
    public interface IProgressBarService
    {
        void Start(int maximun);

        void Stop();
    }
}
