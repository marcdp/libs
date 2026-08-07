using System;


namespace DProjects.Functional {

    public sealed class Result<T, TError> {

        // vars
        private readonly T? mValue;
        private readonly TError? mError;

        // props
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public T Value => IsSuccess ? mValue! : throw new InvalidOperationException("Cannot access Value when result is failure.");
        public TError Error => IsFailure ? mError! : throw new InvalidOperationException("Cannot access Error when result is success.");

        // ctor
        private Result(T value) {
            IsSuccess = true;
            mValue = value;
        }
        private Result(TError error) {
            IsSuccess = false;
            mError = error;
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