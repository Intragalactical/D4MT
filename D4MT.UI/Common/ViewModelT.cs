using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace D4MT.UI.Common;

public class ViewModel<T> where T : IViewModel<T> {
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void NotifyPropertiesChanged(T sender, params string[] propertyNames) {
        foreach (string propertyName in propertyNames) {
            PropertyChanged?.Invoke(sender, new(propertyName));
        }
    }
    protected virtual void NotifyPropertyChanged(T sender, [CallerMemberName] string? propertyName = null) {
        PropertyChanged?.Invoke(sender, new(propertyName));
    }
}
