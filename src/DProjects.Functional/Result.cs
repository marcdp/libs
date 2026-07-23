using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DProjects.Functional {

    public sealed class Result<T, TError> {

        // vars
        private readonly T? _value;
        private readonly TError? _error;

        // props
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access Value when result is failure.");
        public TError Error => IsFailure ? _error! : throw new InvalidOperationException("Cannot access Error when result is success.");

        // ctor
        private Result(T value) {
            IsSuccess = true;
            _value = value;
        }
        private Result(TError error) {
            IsSuccess = false;
            _error = error;
        }

        // static methods
        public static Result<T, TError> Ok(T value) => new(value);

        public static Result<T, TError> Fail(TError error) => new(error);

        // methods
        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<TError, TResult> onFailure) {
            if (onSuccess == null) throw new ArgumentNullException(nameof(onSuccess));
            if (onFailure == null) throw new ArgumentNullException(nameof(onFailure));
            return IsSuccess ? onSuccess(Value) : onFailure(Error);
        }
    }


}