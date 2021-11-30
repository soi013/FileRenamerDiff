namespace UnitTests;

public class ValueHolder_Test
{
    [Fact]
    public void NotifyPropertyChanged()
    {
        var queuePropertyChanged = new Queue<string?>();
        var holder = ValueHolderFactory.Create(string.Empty);

        holder.PropertyChanged += (o, e) => queuePropertyChanged.Enqueue(e.PropertyName);

        //ステージ1 変更前
        holder.Value
            .Should().BeEmpty("初期値は空のはず");

        holder.ToString()
            .Should().NotMatchRegex("\\w", "初期値は空のはず");

        queuePropertyChanged
            .Should().BeEmpty("まだ通知は来ていないはず");

        //ステージ2 変更後
        const string newValue = "NEW_VALUE";
        holder.Value = newValue;

        holder.Value
            .Should().Be(newValue, "新しい値に変わっているはず");

        holder.ToString()
            .Should().Contain(newValue, "新しい値に変わっているはず");

        queuePropertyChanged.Dequeue()
                .Should().Be(nameof(ValueHolder<string>.Value), "Valueプロパティの変更通知があったはず");
    }
}
