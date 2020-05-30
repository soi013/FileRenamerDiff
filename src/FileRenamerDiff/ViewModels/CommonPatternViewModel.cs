using FileRenamerDiff.Models;
using Reactive.Bindings;

namespace FileRenamerDiff.ViewModels
{
    /// <summary>
    /// よく使うパターン集ViewModel
    /// </summary>
    public class CommonPatternViewModel
    {
        protected Model model = Model.Instance;

        private CommonPattern modelPattern;

        /// <summary>
        /// パターン説明
        /// </summary>
        public string Comment => modelPattern.Comment;
        /// <summary>
        /// 置換されるパターン
        /// </summary>
        public string TargetPattern => modelPattern.TargetPattern;
        /// <summary>
        /// 置換後文字列（削除パターンの場合は非表示）
        /// </summary>
        public string ReplaceText => modelPattern.ReplaceText;

        /// <summary>
        /// パターンを単純一致か正規表現とするか
        /// </summary>
        public bool AsExpression => modelPattern.AsExpression;

        /// <summary>
        /// 現在の設定のパターンへの追加
        /// </summary>
        public ReactiveCommand AddSettingCommand { get; } = new ReactiveCommand();

        /// <summary>
        /// 削除パターンか置換パターンか
        /// </summary>
        public bool IsDelete { get; }

        public CommonPatternViewModel(CommonPattern modelPattern, bool isDelete)
        {
            this.modelPattern = modelPattern;
            this.IsDelete = isDelete;

            AddSettingCommand.Subscribe(() =>
                (IsDelete ? model.Setting.DeleteTexts : model.Setting.ReplaceTexts)
                .Add(modelPattern.ToReplacePattern()));
        }
    }
}