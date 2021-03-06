﻿using Livet;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileRenamerDiff.Models
{
    public class ValueHolder<T> : NotificationObject
    {
        private T _Value;
        public T Value
        {
            get => _Value;
            set => RaisePropertyChangedIfSet(ref _Value, value);
        }

        public ValueHolder(T value)
        {
            this._Value = value;
        }
    }
    public static class ValueHolderFactory
    {
        public static ValueHolder<T> Create<T>(T value) => new(value);
    }
}
