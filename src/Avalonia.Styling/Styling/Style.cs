// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Metadata;

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines a style.
    /// </summary>
    public class Style : IStyle
    {
        private static Dictionary<IStyleable, List<IDisposable>> _applied =
            new Dictionary<IStyleable, List<IDisposable>>();

        private IResourceDictionary _resources;

        /// <summary>
        /// Initializes a new instance of the <see cref="Style"/> class.
        /// </summary>
        public Style()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Style"/> class.
        /// </summary>
        /// <param name="selector">The style selector.</param>
        public Style(Func<Selector, Selector> selector)
        {
            Selector = selector(null);
        }

        /// <summary>
        /// Gets or sets a dictionary of style resources.
        /// </summary>
        public IResourceDictionary Resources
        {
            get
            {
                if (_resources == null)
                {
                    _resources = new ResourceDictionary();
                }

                return _resources;
            }
        }

        /// <summary>
        /// Gets or sets the style's selector.
        /// </summary>
        public Selector Selector { get; set; }

        /// <summary>
        /// Gets or sets the style's setters.
        /// </summary>
        [Content]
        public IList<ISetter> Setters { get; set; } = new List<ISetter>();

        /// <summary>
        /// Attaches the style to a control if the style's selector matches.
        /// </summary>
        /// <param name="control">The control to attach to.</param>
        /// <param name="container">
        /// The control that contains this style. May be null.
        /// </param>
        public void Attach(IStyleable control, IStyleHost container)
        {
            if (Selector != null)
            {
                var match = Selector.Match(control);

                if (match.ImmediateResult != false)
                {
                    var subs = GetSubscriptions(control);

                    foreach (var setter in Setters)
                    {
                        var sub = setter.Apply(this, control, match.ObservableResult);
                        subs.Add(sub);
                    }
                }
            }
            else if (control == container)
            {
                var subs = GetSubscriptions(control);

                foreach (var setter in Setters)
                {
                    var sub = setter.Apply(this, control, null);
                    subs.Add(sub);
                }
            }
        }

        /// <inheritdoc/>
        public bool TryGetResource(string key, out object result)
        {
            result = null;
            return _resources?.TryGetResource(key, out result) ?? false;
        }

        /// <summary>
        /// Returns a string representation of the style.
        /// </summary>
        /// <returns>A string representation of the style.</returns>
        public override string ToString()
        {
            if (Selector != null)
            {
                return "Style: " + Selector.ToString();
            }
            else
            {
                return "Style";
            }
        }

        private static List<IDisposable> GetSubscriptions(IStyleable control)
        {
            List<IDisposable> subscriptions;

            if (!_applied.TryGetValue(control, out subscriptions))
            {
                subscriptions = new List<IDisposable>(2);
                subscriptions.Add(control.StyleDetach.Subscribe(ControlDetach));
                _applied.Add(control, subscriptions);
            }

            return subscriptions;
        }

        /// <summary>
        /// Called when a control's <see cref="IStyleable.StyleDetach"/> is signalled to remove
        /// all applied styles.
        /// </summary>
        /// <param name="control">The control.</param>
        private static void ControlDetach(IStyleable control)
        {
            var subscriptions = _applied[control];

            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }

            _applied.Remove(control);
        }
    }
}
