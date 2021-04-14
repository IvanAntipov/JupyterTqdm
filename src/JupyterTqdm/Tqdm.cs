using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JupyterTqdm.Internal;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;

namespace JupyterTqdm
{
    public static class Tqdm
    {
        public static IEnumerable<T> WithProgress<T>(this IEnumerable<T> src,
            [CallerMemberName] string title = "Progress", int? total = null, TimeSpan? updatePeriod = null)
        {
            var actualTotal = ResolveTotal(src, total);
            IProgress ProgressSelector() => new JupyterProgressBar(title, actualTotal);
            return WithProgress<T>(src, ProgressSelector, updatePeriod);
        }
        
        
        
        private static IEnumerable<T> WithProgress<T>(this IEnumerable<T> src,Func<IProgress> progressSelector, TimeSpan? updatePeriod = null)
        {
            var scheduler = CurrentThreadScheduler.Instance;
            var actualUpdatePeriod = updatePeriod ?? TimeSpan.FromSeconds(1);
            IProgress progress = null; 
            var current = new Boxed(0);
            var sub =
                Task.Run(() =>
                    Observable
                        .Interval(actualUpdatePeriod, scheduler)    
                        .Subscribe(_ =>
                            {
                                UpdateSafe(current.Read(), progress);
                            }
                        ));

            progress = progressSelector();
            
            
            void UpdateSafe(long value, IProgress p)
            {
                try
                {
                    p?.Update(value);
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR in Tqdm");
                }
            }
            
            
            try
            {
                foreach (var i in src)
                {
                    current.Increment();
                    yield return i;
                }
                
            }
            finally
            {
                sub.ContinueWith(t => t.Result.Dispose());
            }
            UpdateSafe((int)current.Read(), progress);
            
        }
        private static int? ResolveTotal<T>(IEnumerable<T> src, int? total)
        {
            if (total.HasValue) return total;
            switch (src)
            {
                case IReadOnlyCollection<T>  r : return r.Count;
                default: return null;
            }

        }
        private class Boxed
        {
            private long _current = 0;
            public Boxed(long current)
            {
                _current = current;
            }

            public void Increment() => Interlocked.Increment(ref _current);
            public long Read() => Interlocked.Read(ref _current);

        }
        

        private class JupyterProgressBar:IProgress
        {
            private readonly string _title;
            private readonly int? _total;
            private readonly DisplayedValue _display;

            public JupyterProgressBar(string title, int? total)
            {
                _title = title;
                _total = total;
                var zeroProgress = CreateProgress(0);

                _display = System.DisplayExtensions.Display(zeroProgress);
            }

            public void Start()
            {
                Update(0);   
            }

            private dynamic CreateProgress(long value)
            {
                // TODO Add title
                
                // let createProgress(value: string) =
                // div[]
                // [
                //     label["for", "progress" :> obj][str "Progress: "]
                // progress[_id "progress"; _max "100"; _value value][str "Test"] //.ToString()
                //     ]
                //
                // .html

                return
                    _total.HasValue
                        ? PocketViewTags.progress[max: _total.Value][value: value](_title)
                        : PocketViewTags.progress(_title);
            }
            public void Update(long current)
            {
                _display.Update(CreateProgress(current));
            }
        }
    }
}
