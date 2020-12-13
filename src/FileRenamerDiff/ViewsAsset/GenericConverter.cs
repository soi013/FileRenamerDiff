using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace FileRenamerDiff.Views
{
    /// <summary>
    /// Generic型を使用した汎用コンバーター抽象クラス
    /// </summary>
    /// <typeparam name="TSource">バインディング ソース型</typeparam>
    /// <typeparam name="TTarget">バインディング ターゲット型</typeparam>
    public abstract class GenericConverter<TSource, TTarget> : IValueConverter
    {
        /// <summary>
        /// IValueConverterのConvertメソッド実装（Generic型にキャストして抽象メソッドConvertを呼び出す）
        /// </summary>
        /// <param name="value">バインディング ソースによって生成された値</param>
        /// <param name="targetType">バインディング ターゲット プロパティの型</param>
        /// <param name="parameter">使用するコンバーター パラメーター</param>
        /// <param name="culture">コンバーターで使用するカルチャ</param>
        /// <returns>変換された値</returns>
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => Convert((TSource)value, parameter, culture);

        /// <summary>
        /// Generic型を使用して値変換する抽象メソッド
        /// </summary>
        /// <param name="value">バインディング ソースによって生成された値</param>
        /// <param name="parameter">使用するコンバーター パラメーター</param>
        /// <param name="culture">コンバーターで使用するカルチャ</param>
        /// <returns>変換された値</returns>
        public abstract TTarget Convert(TSource value, object parameter, CultureInfo culture);

        /// <summary>
        /// IValueConverterのConvertBackメソッド実装（Generic型にキャストして抽象メソッドConvertBackを呼び出す）
        /// </summary>
        /// <param name="value">バインディング ターゲットによって生成された値</param>
        /// <param name="targetType">変換後の型</param>
        /// <param name="parameter">使用するコンバーター パラメーター</param>
        /// <param name="culture">コンバーターで使用するカルチャ</param>
        /// <returns>変換された値</returns>
        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => ConvertBack((TTarget)value, parameter, culture);

        /// <summary>
        /// Generic型を使用して値変換する抽象メソッド
        /// </summary>
        /// <param name="value">バインディング ターゲットによって生成された値</param>
        /// <param name="parameter">使用するコンバーター パラメーター</param>
        /// <param name="culture">コンバーターで使用するカルチャ</param>
        /// <returns>変換された値</returns>
        public abstract TSource ConvertBack(TTarget value, object parameter, CultureInfo culture);
    }

}
