using System;
using System.Collections.Generic;
using System.Threading;

namespace DProjects.Log.Provider {

    internal sealed class LoggerScopeContext {
        private readonly AsyncLocal<Scope?> mCurrentScope = new AsyncLocal<Scope?>();

        public IDisposable Push(object state) {
            var scope = new Scope(this, mCurrentScope.Value, state);
            mCurrentScope.Value = scope;
            return scope;
        }

        public IEnumerable<object> GetStates() {
            var states = new List<object>();
            for (var scope = mCurrentScope.Value; scope != null; scope = scope.Parent) {
                if (!scope.IsDisposed) {
                    states.Add(scope.State);
                }
            }
            states.Reverse();
            return states;
        }

        private void Pop(Scope scope) {
            if (ReferenceEquals(mCurrentScope.Value, scope)) {
                var parent = scope.Parent;
                while (parent != null && parent.IsDisposed) {
                    parent = parent.Parent;
                }
                mCurrentScope.Value = parent;
            }
        }

        private sealed class Scope : IDisposable {
            private readonly LoggerScopeContext mContext;
            private int mDisposed;

            public Scope(LoggerScopeContext context, Scope? parent, object state) {
                mContext = context;
                Parent = parent;
                State = state;
            }

            public Scope? Parent { get; }
            public object State { get; }
            public bool IsDisposed => mDisposed != 0;

            public void Dispose() {
                if (Interlocked.Exchange(ref mDisposed, 1) == 0) {
                    mContext.Pop(this);
                }
            }
        }
    }
}
