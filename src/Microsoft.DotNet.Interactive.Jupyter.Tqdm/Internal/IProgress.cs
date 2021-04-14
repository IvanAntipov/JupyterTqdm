using System;

namespace Microsoft.DotNet.Interactive.Jupyter.Tqdm.Internal
{
    public interface IProgress
    {
        void Start();
        void Update(long current);
    }
}