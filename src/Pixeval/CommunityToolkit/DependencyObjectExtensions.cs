﻿#region Copyright (c) Pixeval/Pixeval

// GPL v3 License
// 
// Pixeval/Pixeval
// Copyright (c) 2021 Pixeval/DependencyObjectExtensions.cs
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Pixeval.CommunityToolkit.Predicates;

namespace Pixeval.CommunityToolkit
{
    /// <summary>
    ///     Provides attached dependency properties for the <see cref="DependencyObject" /> type.
    /// </summary>
    public static class DependencyObjectExtensions
    {
        /// <summary>
        ///     Find the first descendant of type <see cref="FrameworkElement" /> with a given name, using a depth-first search.
        /// </summary>
        /// <param name="element">The root element.</param>
        /// <param name="name">The name of the element to look for.</param>
        /// <param name="comparisonType">The comparison type to use to match <paramref name="name" />.</param>
        /// <returns>The descendant that was found, or <see langword="null" />.</returns>
        public static FrameworkElement? FindDescendant(this DependencyObject element, string name, StringComparison comparisonType = StringComparison.Ordinal)
        {
            PredicateByName predicateByName = new(name, comparisonType);

            return FindDescendant<FrameworkElement, PredicateByName>(element, ref predicateByName);
        }

        /// <summary>
        ///     Find the first descendant element of a given type, using a depth-first search.
        /// </summary>
        /// <typeparam name="T">The type of elements to match.</typeparam>
        /// <param name="element">The root element.</param>
        /// <returns>The descendant that was found, or <see langword="null" />.</returns>
        public static T? FindDescendant<T>(this DependencyObject element)
            where T : DependencyObject
        {
            PredicateByAny<T> predicateByAny = default;

            return FindDescendant<T, PredicateByAny<T>>(element, ref predicateByAny);
        }

        /// <summary>
        ///     Find the first descendant element of a given type, using a depth-first search.
        /// </summary>
        /// <param name="element">The root element.</param>
        /// <param name="type">The type of element to match.</param>
        /// <returns>The descendant that was found, or <see langword="null" />.</returns>
        public static DependencyObject? FindDescendant(this DependencyObject element, Type type)
        {
            PredicateByType predicateByType = new(type);

            return FindDescendant<DependencyObject, PredicateByType>(element, ref predicateByType);
        }

        /// <summary>
        ///     Find the first descendant element matching a given predicate, using a depth-first search.
        /// </summary>
        /// <typeparam name="T">The type of elements to match.</typeparam>
        /// <param name="element">The root element.</param>
        /// <param name="predicate">The predicate to use to match the descendant nodes.</param>
        /// <returns>The descendant that was found, or <see langword="null" />.</returns>
        public static T? FindDescendant<T>(this DependencyObject element, Func<T, bool> predicate)
            where T : DependencyObject
        {
            PredicateByFunc<T> predicateByFunc = new(predicate);

            return FindDescendant<T, PredicateByFunc<T>>(element, ref predicateByFunc);
        }

        /// <summary>
        ///     Find the first descendant element matching a given predicate, using a depth-first search.
        /// </summary>
        /// <typeparam name="T">The type of elements to match.</typeparam>
        /// <typeparam name="TState">The type of state to use when matching nodes.</typeparam>
        /// <param name="element">The root element.</param>
        /// <param name="state">The state to give as input to <paramref name="predicate" />.</param>
        /// <param name="predicate">The predicate to use to match the descendant nodes.</param>
        /// <returns>The descendant that was found, or <see langword="null" />.</returns>
        public static T? FindDescendant<T, TState>(this DependencyObject element, TState state, Func<T, TState, bool> predicate)
            where T : DependencyObject
        {
            PredicateByFunc<T, TState> predicateByFunc = new(state, predicate);

            return FindDescendant<T, PredicateByFunc<T, TState>>(element, ref predicateByFunc);
        }

        /// <summary>
        ///     Find the first descendant element matching a given predicate, using a depth-first search.
        /// </summary>
        /// <typeparam name="T">The type of elements to match.</typeparam>
        /// <typeparam name="TPredicate">The type of predicate in use.</typeparam>
        /// <param name="element">The root element.</param>
        /// <param name="predicate">The predicate to use to match the descendant nodes.</param>
        /// <returns>The descendant that was found, or <see langword="null" />.</returns>
        private static T? FindDescendant<T, TPredicate>(this DependencyObject element, ref TPredicate predicate)
            where T : DependencyObject
            where TPredicate : struct, IPredicate<T>
        {
            var childrenCount = VisualTreeHelper.GetChildrenCount(element);

            for (var i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);

                if (child is T result && predicate.Match(result))
                {
                    return result;
                }

                var descendant = FindDescendant<T, TPredicate>(child, ref predicate);

                if (descendant is not null)
                {
                    return descendant;
                }
            }

            return null;
        }

        /// <summary>
        ///     Find the first descendant (or self) of type <see cref="FrameworkElement" /> with a given name, using a depth-first
        ///     search.
        /// </summary>
        /// <param name="element">The root element.</param>
        /// <param name="name">The name of the element to look for.</param>
        /// <param name="comparisonType">The comparison type to use to match <paramref name="name" />.</param>
        /// <returns>The descendant (or self) that was found, or <see langword="null" />.</returns>
        public static FrameworkElement? FindDescendantOrSelf(this DependencyObject element, string name, StringComparison comparisonType = StringComparison.Ordinal)
        {
            return element is FrameworkElement result && name.Equals(result.Name, comparisonType) ? result : FindDescendant(element, name, comparisonType);
        }

        /// <summary>
        ///     Find the first descendant (or self) element of a given type, using a depth-first search.
        /// </summary>
        /// <typeparam name="T">The type of elements to match.</typeparam>
        /// <param name="element">The root element.</param>
        /// <returns>The descendant (or self) that was found, or <see langword="null" />.</returns>
        public static T? FindDescendantOrSelf<T>(this DependencyObject element)
            where T : DependencyObject
        {
            return element is T result ? result : FindDescendant<T>(element);
        }

        /// <summary>
        ///     Find the first descendant (or self) element of a given type, using a depth-first search.
        /// </summary>
        /// <param name="element">The root element.</param>
        /// <param name="type">The type of element to match.</param>
        /// <returns>The descendant (or self) that was found, or <see langword="null" />.</returns>
        public static DependencyObject? FindDescendantOrSelf(this DependencyObject element, Type type)
        {
            return element.GetType() == type ? element : FindDescendant(element, type);
        }

        /// <summary>
        ///     Find the first descendant (or self) element matching a given predicate, using a depth-first search.
        /// </summary>
        /// <typeparam name="T">The type of elements to match.</typeparam>
        /// <param name="element">The root element.</param>
        /// <param name="predicate">The predicate to use to match the descendant nodes.</param>
        /// <returns>The descendant (or self) that was found, or <see langword="null" />.</returns>
        public static T? FindDescendantOrSelf<T>(this DependencyObject element, Func<T, bool> predicate)
            where T : DependencyObject
        {
            return element is T result && predicate(result) ? result : FindDescendant(element, predicate);
        }

        /// <summary>
        ///     Find the first descendant (or self) element matching a given predicate, using a depth-first search.
        /// </summary>
        /// <typeparam name="T">The type of elements to match.</typeparam>
        /// <typeparam name="TState">The type of state to use when matching nodes.</typeparam>
        /// <param name="element">The root element.</param>
        /// <param name="state">The state to give as input to <paramref name="predicate" />.</param>
        /// <param name="predicate">The predicate to use to match the descendant nodes.</param>
        /// <returns>The descendant (or self) that was found, or <see langword="null" />.</returns>
        public static T? FindDescendantOrSelf<T, TState>(this DependencyObject element, TState state, Func<T, TState, bool> predicate)
            where T : DependencyObject
        {
            return element is T result && predicate(result, state) ? result : FindDescendant(element, state, predicate);
        }

        /// <summary>
        ///     Find all descendant elements of the specified element. This method can be chained with
        ///     LINQ calls to add additional filters or projections on top of the returned results.
        ///     <para>
        ///         This method is meant to provide extra flexibility in specific scenarios and it should not
        ///         be used when only the first item is being looked for. In those cases, use one of the
        ///         available <see cref="FindDescendant{T}(DependencyObject)" /> overloads instead, which will
        ///         offer a more compact syntax as well as better performance in those cases.
        ///     </para>
        /// </summary>
        /// <param name="element">The root element.</param>
        /// <returns>All the descendant <see cref="DependencyObject" /> instance from <paramref name="element" />.</returns>
        public static IEnumerable<DependencyObject> FindDescendants(this DependencyObject element)
        {
            var childrenCount = VisualTreeHelper.GetChildrenCount(element);

            for (var i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);

                yield return child;

                foreach (var childOfChild in FindDescendants(child))
                {
                    yield return childOfChild;
                }
            }
        }

        /// <summary>
        ///     Find the first ascendant of type <see cref="FrameworkElement" /> with a given name.
        /// </summary>
        /// <param name="element">The starting element.</param>
        /// <param name="name">The name of the element to look for.</param>
        /// <param name="comparisonType">The comparison type to use to match <paramref name="name" />.</param>
        /// <returns>The ascendant that was found, or <see langword="null" />.</returns>
        public static FrameworkElement? FindAscendant(this DependencyObject element, string name, StringComparison comparisonType = StringComparison.Ordinal)
        {
            PredicateByName predicateByName = new(name, comparisonType);

            return FindAscendant<FrameworkElement, PredicateByName>(element, ref predicateByName);
        }

        /// <summary>
        ///     Find the first ascendant element of a given type.
        /// </summary>
        /// <typeparam name="T">The type of elements to match.</typeparam>
        /// <param name="element">The starting element.</param>
        /// <returns>The ascendant that was found, or <see langword="null" />.</returns>
        public static T? FindAscendant<T>(this DependencyObject element)
            where T : DependencyObject
        {
            PredicateByAny<T> predicateByAny = default;

            return FindAscendant<T, PredicateByAny<T>>(element, ref predicateByAny);
        }

        /// <summary>
        ///     Find the first ascendant element of a given type.
        /// </summary>
        /// <param name="element">The starting element.</param>
        /// <param name="type">The type of element to match.</param>
        /// <returns>The ascendant that was found, or <see langword="null" />.</returns>
        public static DependencyObject? FindAscendant(this DependencyObject element, Type type)
        {
            PredicateByType predicateByType = new(type);

            return FindAscendant<DependencyObject, PredicateByType>(element, ref predicateByType);
        }

        /// <summary>
        ///     Find the first ascendant element matching a given predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements to match.</typeparam>
        /// <param name="element">The starting element.</param>
        /// <param name="predicate">The predicate to use to match the ascendant nodes.</param>
        /// <returns>The ascendant that was found, or <see langword="null" />.</returns>
        public static T? FindAscendant<T>(this DependencyObject element, Func<T, bool> predicate)
            where T : DependencyObject
        {
            PredicateByFunc<T> predicateByFunc = new(predicate);

            return FindAscendant<T, PredicateByFunc<T>>(element, ref predicateByFunc);
        }

        /// <summary>
        ///     Find the first ascendant element matching a given predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements to match.</typeparam>
        /// <typeparam name="TState">The type of state to use when matching nodes.</typeparam>
        /// <param name="element">The starting element.</param>
        /// <param name="state">The state to give as input to <paramref name="predicate" />.</param>
        /// <param name="predicate">The predicate to use to match the ascendant nodes.</param>
        /// <returns>The ascendant that was found, or <see langword="null" />.</returns>
        public static T? FindAscendant<T, TState>(this DependencyObject element, TState state, Func<T, TState, bool> predicate)
            where T : DependencyObject
        {
            PredicateByFunc<T, TState> predicateByFunc = new(state, predicate);

            return FindAscendant<T, PredicateByFunc<T, TState>>(element, ref predicateByFunc);
        }

        /// <summary>
        ///     Find the first ascendant element matching a given predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements to match.</typeparam>
        /// <typeparam name="TPredicate">The type of predicate in use.</typeparam>
        /// <param name="element">The starting element.</param>
        /// <param name="predicate">The predicate to use to match the ascendant nodes.</param>
        /// <returns>The ascendant that was found, or <see langword="null" />.</returns>
        private static T? FindAscendant<T, TPredicate>(this DependencyObject element, ref TPredicate predicate)
            where T : DependencyObject
            where TPredicate : struct, IPredicate<T>
        {
            while (true)
            {
                var parent = VisualTreeHelper.GetParent(element);

                switch (parent)
                {
                    case null:
                        return null;
                    case T result when predicate.Match(result):
                        return result;
                    default:
                        element = parent;
                        break;
                }
            }
        }

        /// <summary>
        ///     Find the first ascendant (or self) of type <see cref="FrameworkElement" /> with a given name.
        /// </summary>
        /// <param name="element">The starting element.</param>
        /// <param name="name">The name of the element to look for.</param>
        /// <param name="comparisonType">The comparison type to use to match <paramref name="name" />.</param>
        /// <returns>The ascendant (or self) that was found, or <see langword="null" />.</returns>
        public static FrameworkElement? FindAscendantOrSelf(this DependencyObject element, string name, StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (element is FrameworkElement result && name.Equals(result.Name, comparisonType))
            {
                return result;
            }

            return FindAscendant(element, name, comparisonType);
        }

        /// <summary>
        ///     Find the first ascendant (or self) element of a given type.
        /// </summary>
        /// <typeparam name="T">The type of elements to match.</typeparam>
        /// <param name="element">The starting element.</param>
        /// <returns>The ascendant (or self) that was found, or <see langword="null" />.</returns>
        public static T? FindAscendantOrSelf<T>(this DependencyObject element)
            where T : DependencyObject
        {
            if (element is T result)
            {
                return result;
            }

            return FindAscendant<T>(element);
        }

        /// <summary>
        ///     Find the first ascendant (or self) element of a given type.
        /// </summary>
        /// <param name="element">The starting element.</param>
        /// <param name="type">The type of element to match.</param>
        /// <returns>The ascendant (or self) that was found, or <see langword="null" />.</returns>
        public static DependencyObject? FindAscendantOrSelf(this DependencyObject element, Type type)
        {
            if (element.GetType() == type)
            {
                return element;
            }

            return FindAscendant(element, type);
        }

        /// <summary>
        ///     Find the first ascendant (or self) element matching a given predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements to match.</typeparam>
        /// <param name="element">The starting element.</param>
        /// <param name="predicate">The predicate to use to match the ascendant nodes.</param>
        /// <returns>The ascendant (or self) that was found, or <see langword="null" />.</returns>
        public static T? FindAscendantOrSelf<T>(this DependencyObject element, Func<T, bool> predicate)
            where T : DependencyObject
        {
            if (element is T result && predicate(result))
            {
                return result;
            }

            return FindAscendant(element, predicate);
        }

        /// <summary>
        ///     Find the first ascendant (or self) element matching a given predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements to match.</typeparam>
        /// <typeparam name="TState">The type of state to use when matching nodes.</typeparam>
        /// <param name="element">The starting element.</param>
        /// <param name="state">The state to give as input to <paramref name="predicate" />.</param>
        /// <param name="predicate">The predicate to use to match the ascendant nodes.</param>
        /// <returns>The ascendant (or self) that was found, or <see langword="null" />.</returns>
        public static T? FindAscendantOrSelf<T, TState>(this DependencyObject element, TState state, Func<T, TState, bool> predicate)
            where T : DependencyObject
        {
            if (element is T result && predicate(result, state))
            {
                return result;
            }

            return FindAscendant(element, state, predicate);
        }

        /// <summary>
        ///     Find all ascendant elements of the specified element. This method can be chained with
        ///     LINQ calls to add additional filters or projections on top of the returned results.
        ///     <para>
        ///         This method is meant to provide extra flexibility in specific scenarios and it should not
        ///         be used when only the first item is being looked for. In those cases, use one of the
        ///         available <see cref="FindAscendant{T}(DependencyObject)" /> overloads instead, which will
        ///         offer a more compact syntax as well as better performance in those cases.
        ///     </para>
        /// </summary>
        /// <param name="element">The root element.</param>
        /// <returns>All the descendant <see cref="DependencyObject" /> instance from <paramref name="element" />.</returns>
        public static IEnumerable<DependencyObject> FindAscendants(this DependencyObject element)
        {
            while (true)
            {
                var parent = VisualTreeHelper.GetParent(element);

                if (parent is null)
                {
                    yield break;
                }

                yield return parent;

                element = parent;
            }
        }
    }
}