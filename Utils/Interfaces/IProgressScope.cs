namespace CreatePipe.Utils.Interfaces
{
    public interface IProgressScope
    {
        // 设置总数（当你搜寻完构件后调用）
        void SetTotal(int total);
        // 更新当前进度
        void Update(int value, string status);
        // 步进（当前值+1）
        void Step(string status);
    }
}
