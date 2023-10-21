using System;
using System.ComponentModel;

namespace D4MT.UI.Common;

public interface IViewModel<T> : INotifyPropertyChanged, IEquatable<T> where T : IViewModel<T> {
    Guid Id { get; }
}
