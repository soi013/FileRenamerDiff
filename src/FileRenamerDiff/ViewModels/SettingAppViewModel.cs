using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using FileRenamerDiff.Models;
using Reactive.Bindings;
using System.Reactive;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Collections;

namespace FileRenamerDiff.ViewModels
{
    public class SettingAppViewModel
    {
        private SettingAppModel setting;
        public ReactivePropertySlim<string> SourceFilePath => setting.SourceFilePath;

        public ObservableCollection<ReactivePropertySlim<string>> IgnoreExtensions => setting.IgnoreExtensions;

        public ObservableCollection<ReplacePattern> DeleteTexts => setting.DeleteTexts;

        public ObservableCollection<ReplacePattern> ReplaceTexts => setting.ReplaceTexts;

        public ReactiveCommand ResetSettingCommand { get; } = new ReactiveCommand();

        /// <summary>
        /// デザイナー用です　コードからは呼べません
        /// </summary>
        [Obsolete("Designer only", true)]
        public SettingAppViewModel() : this(new SettingAppModel()) { }

        public SettingAppViewModel(SettingAppModel setting)
        {
            this.setting = setting;

            ResetSettingCommand.Subscribe(() => Model.Instance.ResetSetting());
        }
    }
}