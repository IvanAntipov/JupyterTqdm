using System;

namespace JupyterTqdm.Internal
{
    public interface IProgress
    {
        void Start();
        void Update(long current);
    }
}