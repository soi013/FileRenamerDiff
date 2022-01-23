using Livet;

namespace FileRenamerDiff.Models;

/// <summary>
/// 変更通知を持つ値を保持するだけのクラス
/// </summary>
public class ValueHolder<T> : NotificationObject
{
    private T _Value;
    /// <summary>
    /// 変更があったら通知する「値」
    /// </summary>
    public T Value
    {
        get => _Value;
        set => RaisePropertyChangedIfSet(ref _Value, value);
    }

    public ValueHolder(T value)
    {
        this._Value = value;
    }

    public override string ToString() => $"[{Value}]";
}

/// <summary>
/// 変更通知を持つ値を保持するだけのクラスのファクトリ
/// </summary>
public static class ValueHolderFactory
{
    /// <summary>
    /// 変更通知を持つ値を保持するだけのオブジェクトの生成
    /// </summary>
    public static ValueHolder<T> Create<T>(T value) => new(value);
}
