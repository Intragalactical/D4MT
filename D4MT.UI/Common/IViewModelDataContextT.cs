namespace D4MT.UI.Common;

public interface IViewModelDataContext<T> where T : IViewModel<T> {
    T DataContext { get; }
}
