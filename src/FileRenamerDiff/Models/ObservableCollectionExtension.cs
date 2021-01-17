using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.IO;
using System.Text.RegularExpressions;

using System.Reactive.Linq;
using Reactive.Bindings.Extensions;
using Anotar.Serilog;
using System.Collections.Specialized;
using Reactive.Bindings;

namespace FileRenamerDiff.Models
{
    public static class ObservableCollectionExtension
    {
        /// <summary>
        /// 指定したコレクションからコピーされた要素を格納するObservableCollectionを生成
        /// </summary>
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source) => new(source);

        /// <summary>
        /// 複数の要素をまとめて追加
        /// </summary>
        public static void AddRange<T>(this ObservableCollection<T> source, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                source.Add(item);
            }
        }

        /// <summary>
        /// 条件に当てはまる要素を除く
        /// </summary>
        /// <returns>1つでも除くものがあったか</returns>
        public static T[] RemoveAll<T>(this ObservableCollection<T> source, Predicate<T> match)
        {
            var removeItems = source
             .Where(x => match(x))
             .ToArray();

            foreach (var item in removeItems)
            {
                source.Remove(item);
            }
            return removeItems;
        }

        /// <summary>
        /// CountプロパティをIObservableとして購読する
        /// </summary>
        public static IObservable<int> ObserveCount<T>(this ObservableCollection<T> source) =>
            source.ObserveProperty(x => x.Count);

        /// <summary>
        /// シークエンスに要素が含まれているかをIObservableとして購読する
        /// </summary>
        public static IObservable<bool> ObserveIsAny<T>(this ObservableCollection<T> source) =>
            source.ObserveCount().Select(x => x >= 1);

        /// <summary>
        /// シークエンスが空かをIObservableとして購読する
        /// </summary>
        public static IObservable<bool> ObserveIsEmpty<T>(this ObservableCollection<T> source) =>
            source.ObserveCount().Select(x => x <= 0);

        /// <summary>
        /// 指定したObservableCollectionと双方向に同期したObservableCollectionを生成する
        /// </summary>
        public static ObservableCollection<TargetT> ToObservableCollctionSynced<SourceT, TargetT>(this ObservableCollection<SourceT> sources,
                Func<SourceT, TargetT> sourceToTarget, Func<TargetT, SourceT> targetToSource)
        {
            //sourcesの要素を変換したコレクションを生成
            var targets = sources.Select(sourceToTarget).ToObservableCollection();

            //2つのコレクションを同期させる
            SyncCollectionTwoWay(sources, targets, sourceToTarget, targetToSource);

            //同期済みのコレクションを返す
            return targets;
        }

        /// <summary>
        /// ２つのObservableCollectionを双方向に同期させる
        /// </summary>
        public static void SyncCollectionTwoWay<SourceT, TargetT>(ObservableCollection<SourceT> sources, ObservableCollection<TargetT> targets,
            Func<SourceT, TargetT> sourceToTarget, Func<TargetT, SourceT> targetToSource)
        {
            bool isChanging = false;

            //Source -> Target
            sources.CollectionChanged += (o, e) =>
                ExcuteIfNotChanging(() => SyncByChangedEventArgs(sources, targets, sourceToTarget, e));

            //Target -> Source
            targets.CollectionChanged += (o, e) =>
                ExcuteIfNotChanging(() => SyncByChangedEventArgs(targets, sources, targetToSource, e));


            //変更イベントループしてしまわないように、ローカル変数(isChanging)でチェック
            //ローカル変数(isChanging)にアクセスするため、ローカル関数で記述
            void ExcuteIfNotChanging(Action action)
            {
                if (isChanging)
                    return;
                isChanging = true;
                action.Invoke();
                isChanging = false;
            }
        }

        private static void SyncByChangedEventArgs<OriginT, DestT>(ObservableCollection<OriginT> origin, ObservableCollection<DestT> dest,
            Func<OriginT, DestT> originToDest, NotifyCollectionChangedEventArgs originE)
        {
            switch (originE.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (originE.NewItems?[0] is OriginT addItem)
                        dest.Insert(originE.NewStartingIndex, originToDest(addItem));
                    return;

                case NotifyCollectionChangedAction.Remove:
                    if (originE.OldStartingIndex >= 0)
                        dest.RemoveAt(originE.OldStartingIndex);
                    return;

                case NotifyCollectionChangedAction.Replace:
                    if (originE.NewItems?[0] is OriginT replaceItem)
                        dest[originE.NewStartingIndex] = originToDest(replaceItem);
                    return;

                case NotifyCollectionChangedAction.Move:
                    dest.Move(originE.OldStartingIndex, originE.NewStartingIndex);
                    return;

                case NotifyCollectionChangedAction.Reset:
                    dest.Clear();
                    foreach (DestT item in origin.Select(originToDest))
                        dest.Add(item);
                    return;
            }
        }
    }
}